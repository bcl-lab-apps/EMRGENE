// (c) Microsoft. All rights reserved
using System.Xml.Serialization;

namespace HealthVault.Foundation
{
    public class AuthSession : IValidatable
    {
        public AuthSession()
        {
        }

        public AuthSession(string token, string personId)
        {
            Token = token;
            Person = new OfflinePersonInfo(personId);
        }

        [XmlElement("auth-token", Order = 1)]
        public string Token { get; set; }

        [XmlElement("offline-person-info", Order = 2)]
        public OfflinePersonInfo Person { get; set; }

        #region IValidatable Members

        public void Validate()
        {
            Token.ValidateRequired("Token");
        }

        #endregion
    }
}