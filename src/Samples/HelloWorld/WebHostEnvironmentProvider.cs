using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using System;
using TaaS;

namespace HelloWorld
{
    internal class WebHostEnvironmentProvider : ITenantServiceProvider<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>
    {
        private readonly IWebHostEnvironment environment;

        public WebHostEnvironmentProvider(IWebHostEnvironment environment)
        {
            if (environment == null)
                throw new ArgumentNullException("environment");
            this.environment = environment;
        }

        public IWebHostEnvironment GetService(TaaS.TenantKey tenant)
        {
            return new TenantHostEnvironment(environment, tenant);
        }

        private class TenantHostEnvironment : IWebHostEnvironment
        {
            private IWebHostEnvironment environment;

            public TenantHostEnvironment(IWebHostEnvironment environment, TaaS.TenantKey tenant)
            {
                this.environment = environment;
            }

            public IFileProvider WebRootFileProvider { get => environment.WebRootFileProvider; set => environment.WebRootFileProvider = value; }
            public string WebRootPath { get => environment.WebRootPath; set => environment.WebRootPath = value; }
            public string ApplicationName { get => environment.ApplicationName; set => environment.ApplicationName = value; }
            public IFileProvider ContentRootFileProvider { get => environment.ContentRootFileProvider; set => environment.ContentRootFileProvider = value; }
            public string ContentRootPath { get => environment.ContentRootPath; set => environment.ContentRootPath = value; }
            public string EnvironmentName { get => environment.EnvironmentName; set => environment.EnvironmentName = value; }
        }
    }
}