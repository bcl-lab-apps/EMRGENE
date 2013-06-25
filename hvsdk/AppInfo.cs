// (c) Microsoft. All rights reserved
using System;
using Windows.System.UserProfile;

namespace HealthVault.Foundation
{
    public class AppInfo : IValidatable
    {
        public AppInfo()
        {
            Language = "en";
            Country = GlobalizationPreferences.HomeGeographicRegion;
            InstanceName = NetworkExtensions.GetMachineName() ?? "Windows 8";
        }

        public Guid MasterAppId { get; set; }

        public string AppName { get; set; }

        public string InstanceName { get; set; }

        public string Country { get; set; }

        public string Language { get; set; }

        #region IValidatable Members

        public void Validate()
        {
            MasterAppId.ValidateRequired("MasterAppId");
            InstanceName.ValidateRequired("InstanceName");
        }

        #endregion
    }
}