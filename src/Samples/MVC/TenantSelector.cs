using Arfilon.TaaS;
using Microsoft.AspNetCore.Http;

namespace MVC
{
    internal class TenantSelector : ITenantSelector
    {
        public TenantKey GetCurrentTenant(HttpContext httpContext)
        {
            return new TenantKey("TestTenant");
        }
    }
}