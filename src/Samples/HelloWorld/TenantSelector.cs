using Microsoft.AspNetCore.Http;
using TaaS;

namespace HelloWorld
{
    internal class TenantSelector : TaaS.ITenantSelector
    {
        public TenantKey GetCurrentTenant(HttpContext httpContext)
        {
            return new TenantKey("SSSaleem");
        }
    }
}