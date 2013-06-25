// (c) Microsoft. All rights reserved
using System;
using System.Xml.Serialization;
using HealthVault.Foundation.Types;

namespace HealthVault.Foundation
{
    [XmlType("header")]
    public class RequestHeader : IValidatable
    {
        private Optional<Guid> m_appId;
        private AuthSession m_session;

        /// <summary>
        /// Required
        /// </summary>
        [XmlElement("method", Order = 1)]
        public string Method { get; set; }

        /// <summary>
        /// Required
        /// </summary>
        [XmlElement("method-version", Order = 2)]
        public int MethodVersion { get; set; }

        [XmlElement("target-person-id", Order = 3)]
        public string PersonId { get; set; }

        [XmlElement("record-id", Order = 4)]
        public string RecordId { get; set; }

        [XmlElement("app-id", Order = 5)]
        public Optional<Guid> AppId
        {
            get { return m_appId; }
            set
            {
                if (value != null)
                {
                    m_session = null;
                }
                m_appId = value;
            }
        }

        [XmlElement("auth-session", Order = 6)]
        public AuthSession Session
        {
            get { return m_session; }
            set
            {
                if (value != null)
                {
                    m_appId = null;
                }
                m_session = value;
            }
        }

        [XmlElement("language", Order = 7)]
        public string Language { get; set; }

        [XmlElement("country", Order = 8)]
        public string Country { get; set; }

        [XmlElement("msg-time", Order = 9)]
        public DateTime Timestamp { get; set; }

        [XmlElement("msg-ttl", Order = 10)]
        public int Ttl { get; set; }

        /// <summary>
        /// Required
        /// </summary>
        [XmlElement("version", Order = 11)]
        public string Version { get; set; }

        [XmlElement("info-hash", Order = 12)]
        public HashData BodyHash { get; set; }

        [XmlIgnore]
        public bool HasRecord
        {
            get { return (RecordId != null); }
        }

        [XmlIgnore]
        public bool HasLanguage
        {
            get { return !string.IsNullOrEmpty(Language); }
        }

        [XmlIgnore]
        public bool HasCountry
        {
            get { return !string.IsNullOrEmpty(Country); }
        }

        [XmlIgnore]
        public bool HasSessionCredentials
        {
            get { return (Session != null); }
        }

        [XmlIgnore]
        public bool HasBodyHash
        {
            get { return (BodyHash != null); }
        }

        #region IValidatable Members

        public void Validate()
        {
            Method.ValidateRequired("Method");
            Session.ValidateOptional();
            BodyHash.ValidateRequired("BodyHash");
        }

        #endregion

        public static RequestHeader CreateDefault()
        {
            var header = new RequestHeader();
            header.MethodVersion = 1;
            header.Version = "WindowsRT V1.0";
            header.Ttl = 1800;
            header.Timestamp = DateTime.UtcNow;

            return header;
        }
    }
}