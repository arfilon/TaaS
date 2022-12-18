using Arfilon.TaaS;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace MVC
{
    internal class TenantSelector : ITenantSelector
    {
        public async Task<TenantKey> GetCurrentTenant(HttpContext httpContext)
        {
            return new TenantKey("TestTenant");
        }
    }
}