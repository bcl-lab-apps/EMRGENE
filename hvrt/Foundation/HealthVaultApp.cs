// (c) Microsoft. All rights reserved

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using HealthVault.Foundation.Types;
using HealthVault.Store;
using Windows.Foundation;
using Windows.Security.Authentication.Web;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.Storage.Streams;

namespace HealthVault.Foundation
{
    public enum AppStartupStatus
    {
        Cancelled = 0,
        Success = 1,
        Pending = 2,
        Failed = 3
    }


    public sealed class HealthVaultApp
    {
        internal const string UserInfoKey = "UserInfo_V1";

        private readonly AppInfoProxy m_appInfo;
        private readonly HealthVaultClient m_client;

        private readonly object m_lock;
        private readonly HealthVaultAppSettings m_appSettings; 
        private readonly ServiceInfoProxy m_serviceInfo;
        private readonly Vocabs m_vocabs;
        private LocalVault m_localVault;
        private AppStartupStatus m_startupStatus;
        private UserInfo m_user;
        
        public HealthVaultApp(HealthVaultAppSettings appSettings)
        {
            m_lock = new object();

            m_appSettings = appSettings;
            m_serviceInfo = new ServiceInfoProxy()
            {
                ShellUrl = appSettings.ShellUrl,
                ServiceUrl = appSettings.ServiceUrl
            };
            m_startupStatus = AppStartupStatus.Cancelled;
            m_appInfo = new AppInfoProxy(appSettings.MasterAppId);

            m_client = new HealthVaultClient(m_appInfo, m_serviceInfo, m_appSettings.UseWebAuthBroker);
            m_localVault = new LocalVault(this, appSettings.Folder, appSettings.Folder);
            m_vocabs = new Vocabs(this);
        }

        public AppStartupStatus StartupStatus
        {
            get { return m_startupStatus; }
        }

        public bool DebugMode
        {
            get { return m_client.Debug; }
            set { m_client.Debug = value; }
        }

        public IAppInfo AppInfo
        {
            get { return m_appInfo; }
        }

        public IServiceInfo ServiceInfo
        {
            get { return m_serviceInfo; }
        }

        /// <summary>
        ///     An app could have been created, but need not have authorized records
        /// </summary>
        public bool IsAppCreated
        {
            get { return m_client.IsProvisioned; }
        }

        /// <summary>
        ///     An app is only truly provisioned if it is both created and has authorized records
        /// </summary>
        public bool IsAppProvisioned
        {
            get { return (m_client.IsProvisioned && HasAuthorizedRecords); }
        }

        public UserInfo UserInfo
        {
            get
            {
                lock (m_lock)
                {
                    return m_user;
                }
            }
            set
            {
                lock (m_lock)
                {
                    if (m_user != null)
                    {
                        m_user.SetClient(null);
                    }

                    m_user = value;
                    if (m_user != null)
                    {
                        m_user.SetClient(m_client);
                    }
                }
            }
        }

        public bool HasAuthorizedRecords
        {
            get
            {
                UserInfo userInfo = UserInfo;
                return (userInfo != null && userInfo.HasAuthorizedRecords);
            }
        }

        public LocalVault LocalVault
        {
            get { return m_localVault; }
        }

        public Vocabs Vocabs
        {
            get { return m_vocabs; }
        }

        public bool HasUserInfo
        {
            get { return (UserInfo != null); }
        }

        internal HealthVaultClient Client
        {
            get { return m_client; }
        }

        public IAsyncAction StartAsync()
        {
            m_startupStatus = AppStartupStatus.Pending;
            return AsyncInfo.Run(cancelToken => EnsureProvisionedAsync(cancelToken));
        }

        public IAsyncAction ResetAsync()
        {
            m_client.ResetState();

            return AsyncInfo.Run(cancelToken => ResetUserInfoAsync(cancelToken));
        }

        public IAsyncOperation<bool> IsAuthorizedOnServerAsync()
        {
            return AsyncInfo.Run(cancelToken => m_client.IsAppAuthorizedOnServerAsync(cancelToken));
        }

        public IAsyncOperation<WebAuthenticationStatus> AuthorizeAdditionalRecordsAsync()
        {
            return AsyncInfo.Run(
                async cancelToken =>
                      {
                          WebAuthenticationStatus status =
                              await m_client.Shell.AppAuthAsync().AsAsyncOperation().AsTask(cancelToken);
                          if (status == WebAuthenticationStatus.Success)
                          {
                              await UpdateUserInfoAsync().AsTask(cancelToken);
                          }

                          return status;
                      }
                );
        }

        public IAsyncAction UpdateUserInfoAsync()
        {
            if (m_startupStatus != AppStartupStatus.Success)
            {
                throw new InvalidOperationException("App not started");
            }

            return AsyncInfo.Run(cancelToken => UpdateUserInfoAsync(cancelToken));
        }

        private async Task EnsureProvisionedAsync(CancellationToken cancelToken)
        {
            m_startupStatus = AppStartupStatus.Pending;

            bool isNewApp = !IsAppCreated;
            try
            {
                m_startupStatus = await EnsureAppCreatedAsync(cancelToken);
            }
            catch
            {
                m_startupStatus = AppStartupStatus.Failed;
                throw;
            }

            if (m_startupStatus != AppStartupStatus.Success)
            {
                await SetUserAndSaveAsync(null, cancelToken);
                return;
            }

            // Set up the encrypted local vault
            if (m_appSettings.UseEncryption)
            {
                var encryptedStore = new EncryptedObjectStore(
                    FolderObjectStore.CreateRoot(m_appSettings.Folder),
                    Client.Cryptographer,
                    Client.State.ProvisioningInfo.SharedSecret);
                m_localVault = new LocalVault(this, FolderObjectStore.CreateRoot(m_appSettings.Folder), encryptedStore);
            }

            // Set the cache setting
            m_localVault.RecordStores.MaxCachedItems = m_appSettings.MaxCachedItems;

            if (!isNewApp)
            {
                await LoadUserInfoAsync(cancelToken);
            }

            if (!HasUserInfo)
            {
                //
                // Download updated Person Information
                //
                await UpdateUserInfoAsync(cancelToken);
            }
        }

        private async Task<AppStartupStatus> EnsureAppCreatedAsync(CancellationToken cancelToken)
        {
            WebAuthenticationStatus authStatus = await m_client.EnsureAppProvisionedAsync(cancelToken);
            return WebAuthenticationStatusToStartupStatus(authStatus);
        }

        private async Task UpdateUserInfoAsync(CancellationToken cancelToken)
        {
            PersonInfo[] person = await m_client.ServiceMethods.GetAuthorizedPersonsAsync(cancelToken);
            UserInfo userInfo = null;
            if (person.IsNullOrEmpty())
            {
                userInfo = null;
            }
            else
            {
                userInfo = new UserInfo(person[0]);
            }

            UserInfo = userInfo;
            await SaveUserInfoAsync(cancelToken);
        }

        private async Task SetUserAndSaveAsync(UserInfo userInfo, CancellationToken cancelToken)
        {
            UserInfo = userInfo;
            await SaveUserInfoAsync(cancelToken);
        }

        private async Task SaveUserInfoAsync(CancellationToken cancelToken)
        {
            string xml = UserInfo != null ? UserInfo.Serialize() : null;
            await m_localVault.RecordRoot.PutAsync(UserInfoKey, xml);
        }

        private async Task LoadUserInfoAsync(CancellationToken cancelToken)
        {
            try
            {
                var xml = (string) await m_localVault.RecordRoot.GetAsync(UserInfoKey, typeof (string));
                if (!string.IsNullOrEmpty(xml))
                {
                    UserInfo = UserInfo.Deserialize(xml);
                    return;
                }
            }
            catch
            {
            }

            UserInfo = null;
        }

        private async Task ResetUserInfoAsync(CancellationToken cancelToken)
        {
            await m_localVault.RecordRoot.DeleteAsync(UserInfoKey);
            UserInfo = null;
        }
        

        private AppStartupStatus WebAuthenticationStatusToStartupStatus(WebAuthenticationStatus authStatus)
        {
            switch (authStatus)
            {
                default:
                    return AppStartupStatus.Failed;

                case WebAuthenticationStatus.Success:
                    return AppStartupStatus.Success;

                case WebAuthenticationStatus.UserCancel:
                    return AppStartupStatus.Cancelled;
            }
        }
    }
}