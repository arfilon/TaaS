using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace Arfilon.TaaS
{
    public delegate void TenentServiceBuilder(IServiceCollection services, TenantKey tenant);
    internal class TenantAppsCollectionConfigration
    {
        private delegate void TenentServiceProviderBuilder(IServiceCollection services, TenantKey tenant, IServiceProvider TaaSProvider);
        private List<TenentServiceProviderBuilder> factories = new List<TenentServiceProviderBuilder>();

        public TenantAppsCollectionConfigration()
        {
        }

        public void ForEachTenant(TenentServiceBuilder serviceBuilder)
        {
            if (serviceBuilder is null)
            {
                throw new ArgumentNullException(nameof(serviceBuilder));
            }

            factories.Add((s, t, _) => serviceBuilder(s, t));
        }

        internal void Build(IServiceCollection services, TenantKey tenant, IServiceProvider TaaSProvider)
        {
            factories.ForEach(t => t(services, tenant, TaaSProvider));

        }

        internal void AddProvider<TService, TServiceProvider>() where TServiceProvider : class, ITenantServiceProvider<TService> where TService : class
        {
            TenentServiceProviderBuilder tenentServiceProvider = (services, tenant, TaaSProvider) =>
            {
                services.AddTransient<TService>(_ =>
                {
                    var factory = TaaSProvider.GetRequiredService<TServiceProvider>();
                    return factory.GetService(tenant);
                });
            };
            factories.Add(tenentServiceProvider);
        }
    }

}