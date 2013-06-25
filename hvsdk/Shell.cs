// (c) Microsoft. All rights reserved
using System;
using System.Threading.Tasks;
using HealthVault.Foundation.Types;
using Windows.Security.Authentication.Web;

namespace HealthVault.Foundation
{
    public class Shell
    {
        private readonly HealthVaultClient m_client;
        private string m_authCompletePage;
        private string m_targetPage;

        public Shell(HealthVaultClient client)
        {
            if (client == null)
            {
                throw new ArgumentNullException("client");
            }

            TargetPage = "redirect.aspx";
            AuthCompletePage = "appContent.aspx";

            m_client = client;
        }

        public string Url
        {
            get { return m_client.ServiceInfo.ShellUrl; }
        }

        public string TargetPage
        {
            get { return m_targetPage; }
            set
            {
                value.ValidateRequired("TargetPage");
                m_targetPage = value;
            }
        }

        public string AuthCompletePage
        {
            get { return m_authCompletePage; }
            set
            {
                value.ValidateRequired("AuthCompletePage");
                m_authCompletePage = value;
            }
        }

        public string UrlForTarget(string target, string queryString)
        {
            target.ValidateRequired("target");

            string fullQs = "target=" + target;
            if (!string.IsNullOrEmpty(queryString))
            {
                fullQs = fullQs + "&targetqs=" + Uri.EscapeDataString(queryString);
            }

            var builder = new UriBuilder(Url);
            builder.Path = TargetPage;
            builder.Query = fullQs;

            return builder.Uri.AbsoluteUri;
        }

        public string UrlForAppProvision()
        {
            m_client.VerifyHasProvisioningInfo();

            AppProvisioningInfo provInfo = m_client.State.ProvisioningInfo;
            AppInfo appInfo = m_client.AppInfo;

            string qs = string.Format(
                "appid={0}&appCreationToken={1}&instanceName={2}&ismra=true",
                appInfo.MasterAppId,
                Uri.EscapeDataString(provInfo.AppCreationToken),
                Uri.EscapeDataString(appInfo.InstanceName));

            return UrlForTarget(Targets.CreateApplication, qs);
        }

        public string UrlForAppAuthSuccess()
        {
            m_client.VerifyHasProvisioningInfo();

            string qs = string.Format(
                "appid={0}&target={1}",
                m_client.State.ProvisioningInfo.AppIdInstance,
                Targets.AppAuthSuccess);

            var builder = new UriBuilder(Url);
            builder.Path = AuthCompletePage;
            builder.Query = qs;

            return builder.Uri.AbsoluteUri;
        }

        public string UrlForAppAuth()
        {
            m_client.VerifyProvisioned();

            string qs = string.Format("appid={0}&ismra=true", m_client.State.ProvisioningInfo.AppIdInstance);
            return UrlForTarget(Targets.AppAuth, qs);
        }

        public async Task<WebAuthenticationStatus> ProvisionApplicationAsync()
        {
            string authUrl = UrlForAppProvision();
            string authSuccessUrl = UrlForAppAuthSuccess();

            return await m_client.WebAuthorizer.AuthAsync(authUrl, authSuccessUrl);
        }

        public async Task<WebAuthenticationStatus> AppAuthAsync()
        {
            string authUrl = UrlForAppAuth();
            string authSuccessUrl = UrlForAppAuthSuccess();

            return await m_client.WebAuthorizer.AuthAsync(authUrl, authSuccessUrl);
        }

        #region Nested type: Targets

        public static class Targets
        {
            public const string CreateApplication = "CREATEAPPLICATION";
            public const string AppAuth = "APPAUTH";
            public const string AppAuthSuccess = "AppAuthSuccess";
        }

        #endregion
    }
}