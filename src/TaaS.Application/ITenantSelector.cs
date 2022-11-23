using System;
using System.Collections.Generic;
using System.Text;

namespace Arfilon.TaaS
{
    public interface ITenantSelector
    {
        TenantKey GetCurrentTenant(Microsoft.AspNetCore.Http.HttpContext httpContext);
    }
}
