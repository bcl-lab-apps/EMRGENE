// (c) Microsoft. All rights reserved

using System;

namespace HealthVault.Foundation
{
    public interface IAppInfo
    {
        string AppId { get; set; }

        string InstanceName { get; set; }

        string Country { get; set; }

        string Language { get; set; }
    }

    internal class AppInfoProxy : AppInfo, IAppInfo
    {
        private string m_appId;

        internal AppInfoProxy(string masterAppId)
        {
            masterAppId.ValidateRequired("masterAppId");
            AppId = masterAppId;
        }

        #region IAppInfo Members

        public string AppId
        {
            get
            {
                if (m_appId == null)
                {
                    m_appId = MasterAppId.ToString("D");
                }
                return m_appId;
            }
            set
            {
                MasterAppId = Guid.Parse(value);
                m_appId = null;
            }
        }

        #endregion
    }
}