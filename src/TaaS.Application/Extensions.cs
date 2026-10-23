using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Logging;
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
            var logger = RootProvider.GetService<ILogger<TenantKey>>();

            app.Use((c) =>
            {
                var dlist = new System.Collections.Concurrent.ConcurrentDictionary<string, RequestDelegate>();

                return async (HttpContext h) =>
                {
                    TenantKey tenant = null;
                    try
                    {
                        var Selector = h.RequestServices.GetRequiredService<ITenantSelector>();
                        if (Selector == null)
                        {
                            logger?.LogError("ITenantSelector not registered. Ensure AddTaaSApplication is called and a tenant selector is configured.");
                            throw new InvalidOperationException("ITenantSelector is not registered in the service collection.");
                        }

                        tenant = await Selector.GetCurrentTenant(h);
                        if (tenant == null)
                        {
                            logger?.LogWarning("Tenant not found for host: {Host}, path: {Path}", h.Request.Host.Value, h.Request.Path);
                            throw new TenantNotFoundException(h.Request.Host.Value);
                        }

                        logger?.LogDebug("Processing request for tenant: {TenantKey}", tenant.Value);

                        var tenantApp = RootProvider.GetServiceForTenant<IApplicationBuilder>(tenant);
                        var next = dlist.GetOrAdd(tenant.Value, (key, a) =>
                        {
                            try
                            {
                                logger?.LogDebug("Building middleware pipeline for tenant: {TenantKey}", tenant.Value);
                                applicationBuilder(tenant, a);
                                return tenantApp.Build();
                            }
                            catch (Exception ex)
                            {
                                logger?.LogError(ex, "Failed to build middleware pipeline for tenant {TenantKey}: {ErrorMessage}", tenant.Value, ex.Message);
                                throw;
                            }
                        }, tenantApp);

                        h.RequestServices = tenantApp.ApplicationServices.CreateScope().ServiceProvider;

                        h.RequestServices.GetRequiredService<Microsoft.AspNetCore.Http.IHttpContextAccessor>().HttpContext = h;
                        await next(h);
                    }
                    catch (TenantNotFoundException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError(ex, "Error processing request for tenant {TenantKey}: {ErrorMessage}", tenant?.Value, ex.Message);
                        throw;
                    }
                };
            });


            return app;
        }
    }
}
