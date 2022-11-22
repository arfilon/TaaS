using Microsoft.AspNetCore.Http;
using TaaS;

namespace Lip
{
    internal class TenantSelector : TaaS.ITenantSelector
    {
        public TenantKey GetCurrentTenant(HttpContext httpContext)
        {
            return new TenantKey("testtrtr");
        }
    }
}