using Arfilon.TaaS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using Arfilon.TaaS.Providers;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Provides extension methods for configuring TaaS multi-tenancy services.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds core TaaS multi-tenancy services to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        /// <remarks>
        /// This method registers the tenant service provider repository and initializes the per-tenant
        /// configuration system. For ASP.NET Core applications, use <see cref="Extensions.AddTaaSApplication"/> instead.
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddTaaS();
        /// services.AddForEachTenant((s, tenant) => {
        ///     s.AddSingleton&lt;IMyService&gt;(new MyService(tenant));
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddTaaS(this IServiceCollection services)
        {
            services.AddSingleton<TenantServiceProviderRepostory>(o =>
            {
                var logger = o.GetService<ILogger<TenantServiceProviderRepostory>>();
                return new TenantServiceProviderRepostory(o, logger);
            });
            services.AddForEachTenant((s, t) =>
            {
            });
            return services;
        }

        /// <summary>
        /// Registers services that should be configured individually for each tenant.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <param name="serviceBuilder">
        /// A delegate that configures services for a specific tenant. This delegate is called
        /// once per tenant when the tenant's service provider is first created.
        /// </param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        /// <remarks>
        /// Services registered via this method are isolated per tenant. Each tenant gets its own
        /// instance of the registered services. This method can be called multiple times to compose
        /// tenant-specific service registrations.
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddForEachTenant((s, tenant) => {
        ///     var connString = $"Server=...;Database=Tenant_{tenant.Value};";
        ///     s.AddDbContext&lt;MyDbContext&gt;(opts => opts.UseSqlServer(connString));
        ///     s.AddSingleton&lt;ITenantConfig&gt;(new TenantConfig(tenant));
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddForEachTenant(this IServiceCollection services, TenentServiceBuilder serviceBuilder)
        {
            services.Configure<TenantAppsCollectionConfigration>(tl =>
            {
                tl.ForEachTenant(serviceBuilder);

            });
            return services;
        }

        /// <summary>
        /// Registers a custom tenant service provider for a specific service type.
        /// </summary>
        /// <typeparam name="TService">The service type to be provided per tenant.</typeparam>
        /// <typeparam name="TServiceProvider">
        /// The implementation of <see cref="ITenantServiceProvider{TService}"/> that creates
        /// tenant-specific instances of <typeparamref name="TService"/>.
        /// </typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        /// <remarks>
        /// This method allows for fine-grained control over how tenant-specific services are created.
        /// The provider is instantiated once and cached, while the services it provides are created
        /// per tenant.
        /// </remarks>
        /// <example>
        /// <code>
        /// public class MyTenantServiceProvider : ITenantServiceProvider&lt;IMyService&gt;
        /// {
        ///     public IMyService GetService(TenantKey tenant)
        ///     {
        ///         return new MyService(tenant.Value);
        ///     }
        /// }
        ///
        /// services.AddTenantServiceProvider&lt;IMyService, MyTenantServiceProvider&gt;();
        /// </code>
        /// </example>
        public static IServiceCollection AddTenantServiceProvider<TService, TServiceProvider>(this IServiceCollection services) where TServiceProvider : class, ITenantServiceProvider<TService> where TService : class
        {
            services.TryAddSingleton<TServiceProvider>();
            services.Configure<TenantAppsCollectionConfigration>(tl =>
            {
                tl.AddProvider<TService, TServiceProvider>();

            });
            return services;
        }

        /// <summary>
        /// Registers a proxy that delegates service resolution to the tenant-specific service provider.
        /// </summary>
        /// <typeparam name="TService">The service type to be proxied per tenant.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        /// <remarks>
        /// This is a convenience method that creates a default proxy for the specified service type.
        /// The proxy retrieves the service from the current tenant's service provider.
        /// Commonly used for framework services like <see cref="System.Diagnostics.DiagnosticSource"/> or
        /// <see cref="Microsoft.Extensions.Hosting.IHostEnvironment"/>.
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddTenantServiceProxy&lt;DiagnosticSource&gt;();
        /// </code>
        /// </example>
        public static IServiceCollection AddTenantServiceProxy<TService>(this IServiceCollection services) /*where TServiceProvider : class, ITenantServiceProvider<TService>*/ where TService : class
        {
            return services.AddTenantServiceProvider<TService, TenantServiceProxyProvider<TService>>();
        }
    }
}

