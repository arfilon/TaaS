using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Arfilon.TaaS.Providers
{
    class TenantApplicationProvider : ITenantServiceProvider<IApplicationBuilder>
    {
        private readonly TenantServiceProviderRepostory providerRepostory;
        private System.Collections.Concurrent.ConcurrentDictionary<string, IApplicationBuilder> blist = new System.Collections.Concurrent.ConcurrentDictionary<string, IApplicationBuilder>();
     

        public TenantApplicationProvider(TenantServiceProviderRepostory providerRepostory)
        {
            if (providerRepostory == null)
                throw new ArgumentNullException("providerRepostory");
            this.providerRepostory = providerRepostory;
        }




        IApplicationBuilder ITenantServiceProvider<IApplicationBuilder>.GetService(TenantKey tenant)
        {
            if (tenant is null)
            {
                throw new ArgumentNullException(nameof(tenant));
            }

            return blist.GetOrAdd(tenant.Value, (key,t) =>
            {
                return new ApplicationBuilder(providerRepostory.GetProvider(t));
            },tenant);
        }


       
    }

}