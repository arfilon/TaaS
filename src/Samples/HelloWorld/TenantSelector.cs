using Microsoft.AspNetCore.Http;
using Arfilon.TaaS;
using System.Threading.Tasks;

namespace HelloWorld
{
    internal class TenantSelector : Arfilon.TaaS.ITenantSelector
    {
        public async Task<TenantKey> GetCurrentTenant(HttpContext httpContext)
        {
            return new TenantKey("testTenantKey");
        }
    }
}