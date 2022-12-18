using Microsoft.AspNetCore.Http;
using Arfilon.TaaS;
using System.Threading.Tasks;

namespace Lip
{
    internal class TenantSelector : ITenantSelector
    {
        public async Task<TenantKey> GetCurrentTenant(HttpContext httpContext)
        {
            return new TenantKey("testTenantKey");
        }
    }
}