using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace Arfilon.TaaS
{
    public class TenantServiceProviderRepostory
    {
        private readonly IServiceProvider TaaSProvider;
        private readonly TenantAppsCollectionConfigration configration;
        private readonly ILogger<TenantServiceProviderRepostory> logger;
        private System.Collections.Concurrent.ConcurrentDictionary<string, IServiceProvider> blist = new System.Collections.Concurrent.ConcurrentDictionary<string, IServiceProvider>();


        internal TenantServiceProviderRepostory(IServiceProvider TaaSProvider, ILogger<TenantServiceProviderRepostory> logger = null)
        {
            if (TaaSProvider == null)
                throw new ArgumentNullException(nameof(TaaSProvider));

            this.TaaSProvider = TaaSProvider;
            this.logger = logger;

            try
            {
                this.configration = this.TaaSProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<TenantAppsCollectionConfigration>>().Value;
                logger?.LogInformation("TenantServiceProviderRepository initialized successfully");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to initialize TenantServiceProviderRepository: {ErrorMessage}", ex.Message);
                throw;
            }
        }

        public IServiceProvider GetProvider(TenantKey tenant)
        {
            if (tenant == null)
            {
                logger?.LogError("GetProvider called with null tenant");
                throw new ArgumentNullException(nameof(tenant));
            }

            try
            {
                return blist.GetOrAdd(tenant.Value, (key) =>
                {
                    logger?.LogInformation("Creating new service provider for tenant: {TenantKey}", tenant.Value);

                    try
                    {
                        IServiceCollection s = new ServiceCollection();
                        configration.Build(s, tenant, TaaSProvider);
                        var provider = s.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

                        logger?.LogInformation("Successfully created service provider for tenant: {TenantKey}", tenant.Value);
                        return provider;
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError(ex, "Failed to build service provider for tenant {TenantKey}: {ErrorMessage}", tenant.Value, ex.Message);
                        throw new InvalidOperationException($"Failed to create service provider for tenant '{tenant.Value}'. See inner exception for details.", ex);
                    }
                });
            }
            catch (Exception ex) when (!(ex is ArgumentNullException))
            {
                logger?.LogError(ex, "Error retrieving service provider for tenant {TenantKey}: {ErrorMessage}", tenant?.Value, ex.Message);
                throw;
            }
        }

    }

}