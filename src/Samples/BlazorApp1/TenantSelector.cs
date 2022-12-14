using Arfilon.TaaS;

internal class TenantSelector : Arfilon.TaaS.ITenantSelector
{
    TenantKey ITenantSelector.GetCurrentTenant(HttpContext httpContext)
    {
        return new TenantKey("yyyyyyyyy");
    }
}