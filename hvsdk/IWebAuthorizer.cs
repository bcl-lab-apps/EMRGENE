// (c) Microsoft. All rights reserved
using System.Threading.Tasks;
using Windows.Security.Authentication.Web;

namespace HealthVault.Foundation
{
    public interface IWebAuthorizer
    {
        Task<WebAuthenticationStatus> AuthAsync(string startUrl, string endUrlPrefix);
    }
}