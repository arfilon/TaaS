using Microsoft.AspNetCore.Http;
using Arfilon.TaaS;

namespace HelloWorld
{
    internal class TenantSelector : Arfilon.TaaS.ITenantSelector
    {
        public TenantKey GetCurrentTenant(HttpContext httpContext)
        {
            return new TenantKey("testTenantKey");
        }
    }
}