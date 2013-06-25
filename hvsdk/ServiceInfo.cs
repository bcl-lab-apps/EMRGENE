// (c) Microsoft. All rights reserved
namespace HealthVault.Foundation
{
    public class ServiceInfo : IValidatable
    {
        /// <summary>
        /// Required
        /// </summary>
        public string ServiceUrl { get; set; }

        /// <summary>
        /// Required
        /// </summary>
        public string ShellUrl { get; set; }

        #region IValidatable Members

        public void Validate()
        {
            ServiceUrl.ValidateRequired("ServiceUrl");
            ShellUrl.ValidateRequired("ShellUrl");
        }

        #endregion

        public void InitForUSPPE()
        {
            ServiceUrl = @"https://platform.healthvault-ppe.com/platform/wildcat.ashx";
            ShellUrl = @"https://account.healthvault-ppe.com";
        }

        public void InitForUSProduction()
        {
            ServiceUrl = @"https://platform.healthvault.com/platform/wildcat.ashx";
            ShellUrl = @"https://account.healthvault.com";
        }

        public void InitForUKPPE()
        {
            ServiceUrl = @"https://platform.healthvault-ppe.co.uk/platform/wildcat.ashx";
            ShellUrl = @"https://account.healthvault-ppe.co.uk";
        }

        public void InitForUKProduction()
        {
            ServiceUrl = @"https://platform.healthvault.co.uk/platform/wildcat.ashx";
            ShellUrl = @"https://account.healthvault.co.uk";
        }
    }
}