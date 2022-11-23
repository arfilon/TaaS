using Arfilon.TaaS;
namespace System
{
    public static class ServiceProviderExtensions
    {
        public static TService GetServiceForTenant<TService>(this System.IServiceProvider provider, TenantKey tenant)
        {
            return (TService)provider.GetServiceForTenant(typeof(TService), tenant);
        }
        public static object GetServiceForTenant(this System.IServiceProvider provider, Type type, TenantKey tenant)
        {
            var tenantServiceProviderRepostory = (TenantServiceProviderRepostory)provider.GetService(typeof(TenantServiceProviderRepostory)) ?? throw new System.InvalidOperationException("this not valid Arfilon.TaaS Provider consider using .AddTaaS()");
            return tenantServiceProviderRepostory.GetProvider(tenant).GetService(type);

        }
    }
}

