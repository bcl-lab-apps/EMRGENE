// (c) Microsoft. All rights reserved

namespace HealthVault.Foundation
{
    public interface IServiceInfo
    {
        string ServiceUrl { get; set; }

        string ShellUrl { get; set; }
    }

    internal class ServiceInfoProxy : ServiceInfo, IServiceInfo
    {
    }
}