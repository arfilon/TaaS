using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using System;
using Arfilon.TaaS;
using Arfilon.TaaS.Providers;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class Extensions
    {
        public static IServiceCollection AddTaaSApplication(this IServiceCollection services)
        {
            services.AddTaaS();

            services.AddTenantServiceProxy<System.Diagnostics.DiagnosticSource>();
            services.AddTenantServiceProxy<System.Diagnostics.DiagnosticListener>();
            services.AddTenantServiceProxy<Microsoft.Extensions.Hosting.IHostEnvironment>();
            services.AddTenantServiceProxy<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();

            services.AddTenantServiceProvider<AspNetCore.Builder.IApplicationBuilder, TenantApplicationProvider>();

            var callingAssembly = Assembly.GetCallingAssembly();

            services.AddForEachTenant((s, tenant) =>
            {
                s.AddHttpContextAccessor();
                s.AddLogging();
                s.AddOptions();
                s.AddSingleton(tenant);
                s.AddMvcCore().AddApplicationPart(callingAssembly);
            });
            return services;
        }

        public static IApplicationBuilder UseForEachTenant(this IApplicationBuilder app, Action<TenantKey, IApplicationBuilder> applicationBuilder)
        {
            var RootProvider = app.ApplicationServices;
            app.Use((c) =>
            {
                var dlist = new System.Collections.Concurrent.ConcurrentDictionary<string, RequestDelegate>();

                return async (HttpContext h) =>
                {
                    var Selector = h.RequestServices.GetRequiredService<ITenantSelector>();
                    var tenant = Selector.GetCurrentTenant(h) ?? throw new TenantNotFoundException(h.Request.Host.Value);

                    h.RequestServices = null;

                    if (tenant != null)
                    {
                        var tenantApp = RootProvider.GetServiceForTenant<IApplicationBuilder>(tenant);
                        var next = dlist.GetOrAdd(tenant.Value, (key, a) =>
                        {
                            applicationBuilder(tenant, a);
                            return tenantApp.Build();
                        }, tenantApp);
                        h.RequestServices = tenantApp.ApplicationServices.CreateScope().ServiceProvider;

                        h.RequestServices.GetRequiredService<Microsoft.AspNetCore.Http.IHttpContextAccessor>().HttpContext = h;
                        await next(h);
                    }
                };
            });


            return app;
        }
    }
}
