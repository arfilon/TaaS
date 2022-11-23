using System;
using System.Collections.Generic;
using System.Text;

namespace Arfilon.TaaS
{
    public interface ITenantServiceProvider<TService>
    {
        TService GetService(TenantKey tenant);
    }
}
