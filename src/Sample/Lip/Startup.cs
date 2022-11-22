using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaaS;

namespace Lip
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTaaSApplication();
            services.AddSingleton<ITenantSelector, TenantSelector>();
            //services.AddControllersWithViews().AddApplicationPart(typeof(MVC.Controllers.HomeController).Assembly);

            services.AddForEachTenant((s, t) =>
            {
                //var mang = new ApplicationPartManager();
                //mang.ApplicationParts.Add(new AssemblyPart(typeof(MVC.Controllers.HomeController).Assembly));
                //s.AddSingleton<ApplicationPartManager>(mang);
                s.AddControllers().AddApplicationPart(typeof(MVC.Controllers.HomeController).Assembly);
    
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }
            //app.UseStaticFiles();

            //app.UseRouting();

            //// app.UseAuthorization();

            //app.UseEndpoints(endpoints =>
            //{
            //    endpoints.MapControllerRoute(
            //        name: "default",
            //        pattern: "{controller=Home}/{action=Index}/{id?}");
            //});
            app.UseForEachTenant((t, a) =>
            {

                a.UseStaticFiles();

                a.UseRouting();

                a.UseAuthorization();

                a.UseEndpoints(endpoints =>
                {
                    endpoints.MapControllerRoute(
                        name: "default",
                        pattern: "{controller=Home}/{action=Index}/{id?}");
                });
            });
        }
    }
}
