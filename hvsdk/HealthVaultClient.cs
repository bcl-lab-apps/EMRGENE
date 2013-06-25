// (c) Microsoft. All rights reserved
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HealthVault.Foundation.Types;
using Windows.Foundation.Collections;
using Windows.Security.Authentication.Web;
using Windows.Storage;

namespace HealthVault.Foundation
{
    public class HealthVaultClient : IDisposable
    {
        private const int DefaultBufferSize = 16*1024; // bytes
        private static ISerializer s_serializer;

        private readonly AppInfo m_appInfo;
        private readonly IWebAuthorizer m_authorizer;
        private readonly ICryptographer m_cryptographer;
        private readonly RecordMethods m_recordMethods;
        private readonly ISecretStore m_secretStore;
        private readonly ServiceInfo m_serviceInfo;
        private readonly ServiceMethods m_serviceMethods;
        private readonly Shell m_shell;
        private readonly IHttpStreamer m_streamer;

        private ClientState m_state;
        private IHttpTransport m_transport;

        static HealthVaultClient()
        {
            s_serializer = new Serializer();
        }

        public HealthVaultClient(AppInfo appInfo, ServiceInfo serviceInfo, bool useWebAuthBroker)
            : this(
                appInfo,
                serviceInfo,
                new HttpTransport(serviceInfo.ServiceUrl),
                new HttpStreamer(),
                new Cryptographer(),
                useWebAuthBroker ? (IWebAuthorizer)new WebAuthorizer() : new BrowserWebAuthorizer())
        {
        }

        public HealthVaultClient(
            AppInfo appInfo,
            ServiceInfo serviceInfo,
            IHttpTransport transport,
            IHttpStreamer streamer,
            ICryptographer cryptographer,
            IWebAuthorizer authorizer)
        {
            appInfo.ValidateRequired("appInfo");
            serviceInfo.ValidateRequired("serviceInfo");
            if (transport == null)
            {
                throw new ArgumentNullException("transport");
            }
            if (streamer == null)
            {
                throw new ArgumentNullException("streamer");
            }
            if (cryptographer == null)
            {
                throw new ArgumentNullException("cryptographer");
            }
            if (authorizer == null)
            {
                throw new ArgumentNullException("authorizer");
            }

            m_appInfo = appInfo;
            m_serviceInfo = serviceInfo;
            m_transport = transport;
            m_streamer = streamer;
            m_cryptographer = cryptographer;
            m_authorizer = authorizer;

            m_serviceMethods = new ServiceMethods(this);
            m_recordMethods = new RecordMethods(this);
            m_shell = new Shell(this);

            m_secretStore = new SecretStore(MakeStoreName(m_appInfo.MasterAppId));
            m_state = new ClientState();
            LoadState();
        }

        public static ISerializer Serializer
        {
            get { return s_serializer; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("Serializer");
                }
                s_serializer = value;
            }
        }

        public AppInfo AppInfo
        {
            get { return m_appInfo; }
        }

        public ServiceInfo ServiceInfo
        {
            get { return m_serviceInfo; }
        }

        public IHttpTransport Transport
        {
            get { return m_transport; }
        }

        public IHttpStreamer Streamer
        {
            get { return m_streamer; }
        }

        public ICryptographer Cryptographer
        {
            get { return m_cryptographer; }
        }

        public IWebAuthorizer WebAuthorizer
        {
            get { return m_authorizer; }
        }

        public ServiceMethods ServiceMethods
        {
            get { return m_serviceMethods; }
        }

        public RecordMethods RecordMethods
        {
            get { return m_recordMethods; }
        }

        public Shell Shell
        {
            get { return m_shell; }
        }

        public ClientState State
        {
            get { return m_state; }
        }

        public bool IsProvisioned
        {
            get { return m_state.IsAppProvisioned; }
        }

        public bool Debug { get; set; }

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        //----------------------------------------
        //
        // EVENTS
        //
        //----------------------------------------
        public event EventHandler<Request> SendingRequest;
        public event EventHandler<Response> ReceivedResponse;
        //
        // Debug support
        // Invoked only if this.Debug is true
        //
        public event Action<object, Request, string> SendingXml;
        public event Action<object, Request, string> ReceivedXml;

        //----------------------------------------
        //
        // Methods
        //
        //----------------------------------------

        public async Task<Response> ExecuteRequestAsync(Request request, Type responseBodyType)
        {
            return await ExecuteRequestAsync(request, responseBodyType, CancellationToken.None);
        }

        public async Task<Response> ExecuteRequestAsync(
            Request request, Type responseBodyType,
            CancellationToken cancelToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            Response response = null;
            int attempt = 1;
            const int maxAttempts = 2;

            while (attempt <= maxAttempts)
            {
                //
                // Ensure we've got a session set up
                //
                SessionCredential credentials = null;
                if (!request.IsAnonymous)
                {
                    credentials = await EnsureCredentialsAsync(cancelToken);
                }
                //
                // Prepare request - adding headers, session & auth information
                //
                PrepareRequestAsync(request, credentials);
                //
                // Notify any subscribers
                //
                NotifySending(request);
                //
                // Call HealthVault
                //
                response = await GetResponseAsync(request, responseBodyType, cancelToken);
                if (
                    response.IsSuccess ||
                        !(response.Status.IsStatusCredentialsExpired || response.Status.IsStatusServerFailure) ||
                        attempt == maxAttempts)
                {
                    break;
                }

                if (response.Status.IsStatusCredentialsExpired)
                {
                    await RefreshSessionTokenAsync(cancelToken);
                }

                ++attempt;
            }

            return response;
        }

        public async Task UpdateProvisioningInfoAsync(CancellationToken cancelToken)
        {
            AppProvisioningInfo provInfo = await m_serviceMethods.GetAppProvisioningInfoAsync(cancelToken);
            lock (m_state)
            {
                m_state.ProvisioningInfo = provInfo;
                SaveState();
            }
        }

        public async Task RefreshSessionTokenAsync(CancellationToken cancelToken)
        {
            SessionCredential credential = await m_serviceMethods.GetSessionTokenAsync(cancelToken);
            lock (m_state)
            {
                m_state.Credentials = credential;
                SaveState();
            }
        }

        public async Task<bool> IsAppAuthorizedOnServerAsync(CancellationToken cancelToken)
        {
            try
            {
                await RefreshSessionTokenAsync(CancellationToken.None);
                return m_state.HasCredentials;
            }
            catch (ServerException se)
            {
                if (!(se.IsStatusCode(ServerStatusCode.InvalidApp) ||
                    se.IsStatusCode(ServerStatusCode.AccessDenied)
                    )
                    )
                {
                    throw;
                }
            }

            return false;
        }

        public async Task<WebAuthenticationStatus> EnsureAppProvisionedAsync(CancellationToken cancelToken)
        {
            if (m_state.IsAppProvisioned)
            {
                return WebAuthenticationStatus.Success;
            }
            //
            // Make sure we've got information to provision this app instance
            //
            if (!m_state.HasProvisioningInfo)
            {
                await UpdateProvisioningInfoAsync(cancelToken);
            }
            //
            // Attempt to create a session. If success, then the app was authorized using Shell
            // Else, we'll need to send the user to Shell
            //
            bool existsOnSever = await IsAppAuthorizedOnServerAsync(cancelToken);
            if (!existsOnSever)
            {
                WebAuthenticationStatus authStatus = await m_shell.ProvisionApplicationAsync();
                if (authStatus != WebAuthenticationStatus.Success)
                {
                    return authStatus;
                }
            }
            await RefreshSessionTokenAsync(cancelToken);

            return m_state.HasCredentials ? WebAuthenticationStatus.Success : WebAuthenticationStatus.UserCancel;
        }

        public async Task<SessionCredential> EnsureCredentialsAsync(CancellationToken cancelToken)
        {
            if (!m_state.HasCredentials)
            {
                await RefreshSessionTokenAsync(cancelToken);
            }

            return m_state.Credentials;
        }

        //----------------------------------------
        //
        // State mgmt
        //
        //----------------------------------------
        public void LoadState()
        {
            lock (m_state)
            {
                try
                {
                    ClientState state = ClientState.Load(m_secretStore);
                    m_state = state;
                }
                catch
                {
                }
            }
        }

        public void SaveState()
        {
            lock (m_state)
            {
                m_state.Save(m_secretStore);
            }
        }

        public void ResetState()
        {
            lock (m_state)
            {
                m_state.Reset(m_secretStore);
            }
        }

        public IPropertySet GetPropertyStore()
        {
            return ApplicationData.Current.GetLocalPropertySet(MakeStoreName(m_appInfo.MasterAppId));
        }

        //----------------------------------------
        //
        // Implementation
        //
        //----------------------------------------

        private async Task<Response> GetResponseAsync(
            Request request, Type responseBodyType,
            CancellationToken cancelToken)
        {
            //
            // Serialize the request
            //
            StringContent content = SerializeRequest(request);
            //
            // Call the server. 
            //
            HttpResponseMessage httpResponse = await m_transport.SendAsync(content, cancelToken);
            using (httpResponse)
            {
                //
                // Deserialize the response
                //
                if (Debug)
                {
                    return await DeserializeResponseDebug(request, httpResponse.Content, responseBodyType);
                }

                return await DeserializeResponse(request, httpResponse.Content, responseBodyType);
            }
        }

        private void PrepareRequestAsync(Request request, SessionCredential credentials)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            PrepareHeader(request, credentials);
            PrepareAuth(request, credentials);

            request.Validate();
        }

        private void PrepareHeader(Request request, SessionCredential credentials)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            EnsureStandardHeaders(request);
            EnsureBodyHash(request);
            EnsureSession(request, credentials);
        }

        private void PrepareAuth(Request request, SessionCredential credentials)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }
            if (request.IsAnonymous)
            {
                return;
            }
            if (credentials == null)
            {
                throw new ArgumentNullException("credentials");
            }

            VerifyCredentials();

            string headerXml = request.Header.ToXml();
            Hmac hmac = m_cryptographer.Hmac(credentials.SharedSecret, headerXml);
            request.Auth = new RequestAuth(hmac);
        }

        private void EnsureStandardHeaders(Request request)
        {
            RequestHeader header = request.Header;
            if (request.Record != null)
            {
                header.RecordId = request.Record.RecordId;
            }
            if (!header.HasLanguage)
            {
                header.Language = m_appInfo.Language;
            }
            if (!header.HasCountry)
            {
                header.Country = m_appInfo.Country;
            }
        }

        private void EnsureBodyHash(Request request)
        {
            if (!request.Header.HasBodyHash)
            {
                request.Header.BodyHash = new HashData(request.Body.Hash(m_cryptographer));
            }
        }

        private void EnsureSession(Request request, SessionCredential credentials)
        {
            if (credentials == null)
            {
                return;
            }

            var session = new AuthSession();
            session.Token = credentials.Token;
            if (request.Record != null)
            {
                session.Person = new OfflinePersonInfo(request.Record.PersonId);
            }

            request.Header.Session = session;
        }

        private StringContent SerializeRequest(Request request)
        {
            string xml;
            using (var writer = new StringWriter())
            {
                Serializer.Serialize(writer, request, null);
                xml = writer.ToString();
            }

            NotifySending(request, xml);

            return new StringContent(xml);
        }

        private async Task<Response> DeserializeResponse(Request request, HttpContent content, Type bodyType)
        {
            using (Stream contentStream = await content.ReadAsStreamAsync())
            {
                using (var reader = new StreamReader(contentStream, Encoding.UTF8, false, DefaultBufferSize, true))
                {
                    return DeserializeResponseXml(request, reader, bodyType);
                }
            }
        }

        private async Task<Response> DeserializeResponseDebug(Request request, HttpContent content, Type bodyType)
        {
            string xml = await content.ReadAsStringAsync();
            NotifyReceived(request, xml);

            using (var reader = new StringReader(xml))
            {
                return DeserializeResponseXml(request, reader, bodyType);
            }
        }

        private Response DeserializeResponseXml(Request request, TextReader reader, Type bodyType)
        {
            var context = new ResponseDeserializationContext {BodyType = bodyType};

            var response = (Response) Serializer.Deserialize(reader, typeof (Response), context);
            response.Request = request;

            NotifyReceived(response);

            return response;
        }

        private string MakeStoreName(Guid masterAppId)
        {
            return string.Format("HealthVaultApp_{0}", masterAppId.ToString("D"));
        }

        internal void VerifyProvisioned()
        {
            if (!m_state.IsAppProvisioned)
            {
                throw new ClientException(ClientError.AppNotProvisioned);
            }
        }

        internal void VerifyHasProvisioningInfo()
        {
            if (!m_state.HasProvisioningInfo)
            {
                throw new ClientException(ClientError.NoProvisioningInfo);
            }
        }

        internal void VerifyCredentials()
        {
            if (!m_state.HasCredentials)
            {
                throw new ClientException(ClientError.NoCredentials);
            }
        }

        private void NotifySending(Request request)
        {
            if (SendingRequest != null)
            {
                SendingRequest(this, request);
            }
        }

        private void NotifySending(Request request, string xml)
        {
            if (SendingXml != null)
            {
                SendingXml(this, request, xml);
            }
        }

        private void NotifyReceived(Response response)
        {
            if (ReceivedResponse != null)
            {
                ReceivedResponse(this, response);
            }
        }

        private void NotifyReceived(Request request, string xml)
        {
            if (ReceivedXml != null)
            {
                ReceivedXml(this, request, xml);
            }
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (m_transport != null)
                {
                    m_transport.Dispose();
                    m_transport = null;
                }
            }
        }
    }
}