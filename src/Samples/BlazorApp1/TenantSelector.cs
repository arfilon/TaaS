using Arfilon.TaaS;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

internal class TenantSelector : Arfilon.TaaS.ITenantSelector
{
    async Task<TenantKey> ITenantSelector.GetCurrentTenant(HttpContext httpContext)
    {
        return new TenantKey("yyyyyyyyy");
    }
}