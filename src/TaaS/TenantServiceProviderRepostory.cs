using Microsoft.Extensions.DependencyInjection;
using System;

namespace Arfilon.TaaS
{
    public class TenantServiceProviderRepostory
    {
        private readonly IServiceProvider TaaSProvider;
        private readonly TenantAppsCollectionConfigration configration;
        private System.Collections.Concurrent.ConcurrentDictionary<string, IServiceProvider> blist = new System.Collections.Concurrent.ConcurrentDictionary<string, IServiceProvider>();
       

        internal TenantServiceProviderRepostory(IServiceProvider TaaSProvider)
        {
            if (TaaSProvider == null)
                throw new ArgumentNullException("TaaSProvider");
            this.TaaSProvider = TaaSProvider;
            this.configration = this.TaaSProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<TenantAppsCollectionConfigration>>().Value ;
        }
        public IServiceProvider GetProvider(TenantKey tenant)
        {
            return blist.GetOrAdd(tenant.Value, (key) =>
            {
                IServiceCollection s = new ServiceCollection();
                configration.Build(s, tenant, TaaSProvider);
                return s.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = false, ValidateScopes = true });
            });
        }

    }

}