using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Arfilon.TaaS
{
    public interface ITenantSelector
    {
        Task<TenantKey> GetCurrentTenant(Microsoft.AspNetCore.Http.HttpContext httpContext);
    }
}
