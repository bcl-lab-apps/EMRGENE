// (c) Microsoft. All rights reserved
using System.Xml.Serialization;
using HealthVault.Foundation.Types;

namespace HealthVault.Foundation
{
    public class ClientState
    {
        public const string StateKeyName = "Auth";

        private readonly object m_lock;
        private SessionCredential m_credentials;
        private AppProvisioningInfo m_provInfo;

        public ClientState()
        {
            m_lock = new object();
        }

        [XmlElement]
        public AppProvisioningInfo ProvisioningInfo
        {
            get
            {
                lock (m_lock)
                {
                    return m_provInfo;
                }
            }
            set
            {
                lock (m_lock)
                {
                    m_provInfo = value;
                }
            }
        }

        [XmlElement]
        public SessionCredential Credentials
        {
            get
            {
                lock (m_lock)
                {
                    return m_credentials;
                }
            }
            set
            {
                lock (m_lock)
                {
                    m_credentials = value;
                }
            }
        }

        [XmlIgnore]
        public bool HasProvisioningInfo
        {
            get
            {
                lock (m_lock)
                {
                    return (m_provInfo != null && m_provInfo.IsValid);
                }
            }
        }

        [XmlIgnore]
        public bool HasCredentials
        {
            get
            {
                lock (m_lock)
                {
                    return (m_credentials != null && m_credentials.IsValid);
                }
            }
        }

        [XmlIgnore]
        public bool IsAppProvisioned
        {
            get
            {
                lock (m_lock)
                {
                    return (HasProvisioningInfo && HasCredentials);
                }
            }
        }

        public void Save(ISecretStore store)
        {
            store.ValidateRequired("store");
            lock (m_lock)
            {
                string xml = this.ToXml();
                store.PutSecret(StateKeyName, xml);
            }
        }

        public static ClientState Load(ISecretStore store)
        {
            store.ValidateRequired("store");
            string xml = store.GetSecret(StateKeyName);
            if (string.IsNullOrEmpty(xml))
            {
                return new ClientState();
            }

            return HealthVaultClient.Serializer.FromXml<ClientState>(xml);
        }

        public void Reset(ISecretStore store)
        {
            store.ValidateRequired("store");

            lock (m_lock)
            {
                Clear();
                store.RemoveSecret(StateKeyName);
            }
        }

        private void Clear()
        {
            if (m_provInfo != null)
            {
                m_provInfo.Clear();
            }
            if (m_credentials != null)
            {
                m_credentials.Clear();
            }
        }
    }
}