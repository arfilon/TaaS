using Microsoft.AspNetCore.Http;
using Arfilon.TaaS;

namespace Lip
{
    internal class TenantSelector : ITenantSelector
    {
        public TenantKey GetCurrentTenant(HttpContext httpContext)
        {
            return new TenantKey("testTenantKey");
        }
    }
}