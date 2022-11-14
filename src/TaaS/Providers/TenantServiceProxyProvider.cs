using System;

namespace TaaS.Providers
{
    class TenantServiceProxyProvider<TType> : ITenantServiceProvider<TType> where TType:class
    {
        private readonly IServiceProvider serviceProvider;

        public TenantServiceProxyProvider(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
                throw new ArgumentNullException("serviceProvider");
            this.serviceProvider = serviceProvider;
        }

        TType ITenantServiceProvider<TType>.GetService(TenantKey tenant)
        {
            return (TType) serviceProvider.GetService(typeof(TType)) ?? throw new NullReferenceException($"Can't resolve '{typeof(TType).FullName}' from '{this.GetType().FullName} for '{tenant.Value}'.");
        }
    }
}
