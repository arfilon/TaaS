using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HelloWorld
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTaaSApplication();
            services.AddSingleton<Arfilon.TaaS.ITenantSelector, TenantSelector>();
            services.AddForEachTenant((s, t) =>
            {
                s.AddRouting();
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseForEachTenant((t, a) =>
            {

                a.UseRouting();
                a.UseEndpoints(endpoints =>
                {
                    endpoints.MapGet("/", async context =>
                    {
                        await context.Response.WriteAsync("Hello World! From " + t.Value);
                    });
                });
            });
        }
    }
}
