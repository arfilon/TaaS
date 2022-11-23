using Arfilon.TaaS;
using Microsoft.Extensions.DependencyInjection;
using System;
using Arfilon.TaaS.Providers;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {

        public static IServiceCollection AddTaaS(this IServiceCollection services)
        {
            services.AddSingleton<TenantServiceProviderRepostory>(o => new TenantServiceProviderRepostory(o));
            services.AddForEachTenant((s, t) =>
            {
            });
            return services;
        }

        public static IServiceCollection AddForEachTenant(this IServiceCollection services, TenentServiceBuilder serviceBuilder)
        {
            services.Configure<TenantAppsCollectionConfigration>(tl =>
            {
                tl.ForEachTenant(serviceBuilder);

            });
            return services;
        }
        public static IServiceCollection AddTenantServiceProvider<TService, TServiceProvider>(this IServiceCollection services) where TServiceProvider : class, ITenantServiceProvider<TService> where TService : class
        {
            services.AddTransient<TServiceProvider>();
            services.Configure<TenantAppsCollectionConfigration>(tl =>
            {
                tl.AddProvider<TService, TServiceProvider>();

            });
            return services;
        }
        public static IServiceCollection AddTenantServiceProxy<TService>(this IServiceCollection services) /*where TServiceProvider : class, ITenantServiceProvider<TService>*/ where TService : class
        {
            return services.AddTenantServiceProvider<TService, TenantServiceProxyProvider<TService>>();
        }
    }
}

