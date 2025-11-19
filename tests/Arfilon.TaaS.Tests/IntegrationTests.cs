using System;
using System.Threading.Tasks;
using Arfilon.TaaS;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Arfilon.TaaS.Tests
{
    public class IntegrationTests
    {
        private class TestTenantSelector : ITenantSelector
        {
            private readonly Func<HttpContext, Task<TenantKey>> selector;

            public TestTenantSelector(Func<HttpContext, Task<TenantKey>> selector)
            {
                this.selector = selector;
            }

            public Task<TenantKey> GetCurrentTenant(HttpContext httpContext)
            {
                return selector(httpContext);
            }
        }

        [Fact]
        public async Task Middleware_WithValidTenant_ProcessesRequest()
        {
            // Arrange
            using var host = await new HostBuilder()
                .ConfigureWebHost(webBuilder =>
                {
                    webBuilder
                        .UseTestServer()
                        .ConfigureServices(services =>
                        {
                            services.AddTaaSApplication();
                            services.AddSingleton<ITenantSelector>(
                                new TestTenantSelector(_ => Task.FromResult(new TenantKey("tenant1"))));
                        })
                        .Configure(app =>
                        {
                            app.UseForEachTenant((tenant, tenantApp) =>
                            {
                                tenantApp.Run(async context =>
                                {
                                    await context.Response.WriteAsync($"Tenant: {tenant.Value}");
                                });
                            });
                        });
                })
                .StartAsync();

            var client = host.GetTestClient();

            // Act
            var response = await client.GetAsync("/");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
            content.Should().Be("Tenant: tenant1");
        }

        [Fact]
        public async Task Middleware_WithDifferentTenants_IsolatesRequests()
        {
            // Arrange
            var currentTenant = "tenant1";
            using var host = await new HostBuilder()
                .ConfigureWebHost(webBuilder =>
                {
                    webBuilder
                        .UseTestServer()
                        .ConfigureServices(services =>
                        {
                            services.AddTaaSApplication();
                            services.AddSingleton<ITenantSelector>(
                                new TestTenantSelector(_ => Task.FromResult(new TenantKey(currentTenant))));
                        })
                        .Configure(app =>
                        {
                            app.UseForEachTenant((tenant, tenantApp) =>
                            {
                                tenantApp.Run(async context =>
                                {
                                    var injectedTenant = context.RequestServices.GetRequiredService<TenantKey>();
                                    await context.Response.WriteAsync($"Tenant: {injectedTenant.Value}");
                                });
                            });
                        });
                })
                .StartAsync();

            var client = host.GetTestClient();

            // Act
            var response1 = await client.GetAsync("/");
            var content1 = await response1.Content.ReadAsStringAsync();

            currentTenant = "tenant2";
            var response2 = await client.GetAsync("/");
            var content2 = await response2.Content.ReadAsStringAsync();

            // Assert
            content1.Should().Be("Tenant: tenant1");
            content2.Should().Be("Tenant: tenant2");
        }

        [Fact]
        public async Task Middleware_WithNoTenant_ThrowsTenantNotFoundException()
        {
            // Arrange
            using var host = await new HostBuilder()
                .ConfigureWebHost(webBuilder =>
                {
                    webBuilder
                        .UseTestServer()
                        .ConfigureServices(services =>
                        {
                            services.AddTaaSApplication();
                            services.AddSingleton<ITenantSelector>(
                                new TestTenantSelector(_ => Task.FromResult<TenantKey>(null)));
                        })
                        .Configure(app =>
                        {
                            app.UseForEachTenant((tenant, tenantApp) =>
                            {
                                tenantApp.Run(async context =>
                                {
                                    await context.Response.WriteAsync("Should not reach here");
                                });
                            });
                        });
                })
                .StartAsync();

            var client = host.GetTestClient();

            // Act
            Func<Task> act = async () => await client.GetAsync("/");

            // Assert
            await act.Should().ThrowAsync<TenantNotFoundException>();
        }

        [Fact]
        public async Task Middleware_WithPerTenantServices_IsolatesServices()
        {
            // Arrange
            var currentTenant = "tenant1";
            using var host = await new HostBuilder()
                .ConfigureWebHost(webBuilder =>
                {
                    webBuilder
                        .UseTestServer()
                        .ConfigureServices(services =>
                        {
                            services.AddTaaSApplication();
                            services.AddSingleton<ITenantSelector>(
                                new TestTenantSelector(_ => Task.FromResult(new TenantKey(currentTenant))));

                            services.AddForEachTenant((s, tenant) =>
                            {
                                s.AddSingleton<string>($"Config for {tenant.Value}");
                            });
                        })
                        .Configure(app =>
                        {
                            app.UseForEachTenant((tenant, tenantApp) =>
                            {
                                tenantApp.Run(async context =>
                                {
                                    var config = context.RequestServices.GetRequiredService<string>();
                                    await context.Response.WriteAsync(config);
                                });
                            });
                        });
                })
                .StartAsync();

            var client = host.GetTestClient();

            // Act
            var response1 = await client.GetAsync("/");
            var content1 = await response1.Content.ReadAsStringAsync();

            currentTenant = "tenant2";
            var response2 = await client.GetAsync("/");
            var content2 = await response2.Content.ReadAsStringAsync();

            // Assert
            content1.Should().Be("Config for tenant1");
            content2.Should().Be("Config for tenant2");
        }

        [Fact]
        public async Task Middleware_ConcurrentRequests_HandlesThreadSafely()
        {
            // Arrange
            var requestCount = 0;
            using var host = await new HostBuilder()
                .ConfigureWebHost(webBuilder =>
                {
                    webBuilder
                        .UseTestServer()
                        .ConfigureServices(services =>
                        {
                            services.AddTaaSApplication();
                            services.AddSingleton<ITenantSelector>(
                                new TestTenantSelector(ctx =>
                                {
                                    var tenantId = System.Threading.Interlocked.Increment(ref requestCount);
                                    return Task.FromResult(new TenantKey($"tenant{tenantId % 3 + 1}"));
                                }));
                        })
                        .Configure(app =>
                        {
                            app.UseForEachTenant((tenant, tenantApp) =>
                            {
                                tenantApp.Run(async context =>
                                {
                                    var injectedTenant = context.RequestServices.GetRequiredService<TenantKey>();
                                    await context.Response.WriteAsync($"{injectedTenant.Value}");
                                });
                            });
                        });
                })
                .StartAsync();

            var client = host.GetTestClient();

            // Act
            var tasks = new Task<string>[100];
            for (int i = 0; i < 100; i++)
            {
                tasks[i] = Task.Run(async () =>
                {
                    var response = await client.GetAsync("/");
                    return await response.Content.ReadAsStringAsync();
                });
            }

            var results = await Task.WhenAll(tasks);

            // Assert
            results.Should().HaveCount(100);
            results.Should().OnlyContain(r => r == "tenant1" || r == "tenant2" || r == "tenant3");
        }
    }
}
