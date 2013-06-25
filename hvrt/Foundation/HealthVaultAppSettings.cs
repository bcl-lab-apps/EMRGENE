// (c) Microsoft. All rights reserved

using Windows.Storage;

namespace HealthVault.Foundation
{
    public sealed class HealthVaultAppSettings
    {
        public HealthVaultAppSettings(string masterAppId)
            : this(masterAppId, false, true)
        {
        }

        public HealthVaultAppSettings(string masterAppId, bool production, bool isUSInstance)
            : this(masterAppId, production, isUSInstance, false, 0, false, ApplicationData.Current.LocalFolder)
        {
        }

        public HealthVaultAppSettings(
            string masterAppId,
            bool production,
            bool isUSInstance,
            bool useEncryption,
            int maxCachedItems,
            bool useWebAuthBroker,
            StorageFolder folder)
        {
            MasterAppId = masterAppId;
            UseEncryption = useEncryption;
            MaxCachedItems = maxCachedItems;
            UseWebAuthBroker = useWebAuthBroker;
            Folder = folder;

            ServiceInfo serviceInfo = CreateServiceInfo(production, isUSInstance);
            ServiceUrl = serviceInfo.ServiceUrl;
            ShellUrl = serviceInfo.ShellUrl;
        }
        
        public string MasterAppId { get; set; }
        
        public int MaxCachedItems { get; set; }

        public bool UseEncryption { get; set; }

        public bool UseWebAuthBroker { get; set; }

        public string ServiceUrl { get; set; }

        public string ShellUrl { get; set; }

        public StorageFolder Folder { get; set; }

        private static ServiceInfo CreateServiceInfo(
            bool production,
            bool isUSInstance)
        {
            var serviceInfo = new ServiceInfo();

            if (production)
            {
                if (isUSInstance)
                {
                    serviceInfo.InitForUSProduction();
                }
                else
                {
                    serviceInfo.InitForUKProduction();
                }
            }
            else
            {
                if (isUSInstance)
                {
                    serviceInfo.InitForUSPPE();
                }
                else
                {
                    serviceInfo.InitForUKPPE();
                }
            }

            return serviceInfo;
        }
    }
}
