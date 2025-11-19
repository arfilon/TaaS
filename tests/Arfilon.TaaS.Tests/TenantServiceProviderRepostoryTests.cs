using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arfilon.TaaS;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Arfilon.TaaS.Tests
{
    public class TenantServiceProviderRepostoryTests
    {
        private interface ITestService
        {
            string GetValue();
        }

        private class TestService : ITestService
        {
            private readonly string value;
            public TestService(string value) => this.value = value;
            public string GetValue() => value;
        }

        [Fact]
        public void Constructor_WithValidProvider_CreatesInstance()
        {
            // Arrange
            var services = new ServiceCollection();
            services.Configure<TenantAppsCollectionConfigration>(config => { });
            var provider = services.BuildServiceProvider();

            // Act
            var repository = new TenantServiceProviderRepostory(provider);

            // Assert
            repository.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_WithNullProvider_ThrowsArgumentNullException()
        {
            // Act
            Action act = () => new TenantServiceProviderRepostory(null);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("TaaSProvider");
        }

        [Fact]
        public void GetProvider_WithValidTenant_ReturnsServiceProvider()
        {
            // Arrange
            var services = new ServiceCollection();
            services.Configure<TenantAppsCollectionConfigration>(config => { });
            var provider = services.BuildServiceProvider();
            var repository = new TenantServiceProviderRepostory(provider);
            var tenant = new TenantKey("tenant1");

            // Act
            var tenantProvider = repository.GetProvider(tenant);

            // Assert
            tenantProvider.Should().NotBeNull();
        }

        [Fact]
        public void GetProvider_WithNullTenant_ThrowsArgumentNullException()
        {
            // Arrange
            var services = new ServiceCollection();
            services.Configure<TenantAppsCollectionConfigration>(config => { });
            var provider = services.BuildServiceProvider();
            var repository = new TenantServiceProviderRepostory(provider);

            // Act
            Action act = () => repository.GetProvider(null);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("tenant");
        }

        [Fact]
        public void GetProvider_CalledMultipleTimes_ReturnsSameCachedProvider()
        {
            // Arrange
            var services = new ServiceCollection();
            services.Configure<TenantAppsCollectionConfigration>(config => { });
            var provider = services.BuildServiceProvider();
            var repository = new TenantServiceProviderRepostory(provider);
            var tenant = new TenantKey("tenant1");

            // Act
            var provider1 = repository.GetProvider(tenant);
            var provider2 = repository.GetProvider(tenant);

            // Assert
            provider1.Should().BeSameAs(provider2);
        }

        [Fact]
        public void GetProvider_DifferentTenants_ReturnsDifferentProviders()
        {
            // Arrange
            var services = new ServiceCollection();
            services.Configure<TenantAppsCollectionConfigration>(config => { });
            var provider = services.BuildServiceProvider();
            var repository = new TenantServiceProviderRepostory(provider);
            var tenant1 = new TenantKey("tenant1");
            var tenant2 = new TenantKey("tenant2");

            // Act
            var provider1 = repository.GetProvider(tenant1);
            var provider2 = repository.GetProvider(tenant2);

            // Assert
            provider1.Should().NotBeSameAs(provider2);
        }

        [Fact]
        public void GetProvider_WithTenantConfiguration_AppliesConfiguration()
        {
            // Arrange
            var services = new ServiceCollection();
            services.Configure<TenantAppsCollectionConfigration>(config =>
            {
                config.ForEachTenant((s, t) =>
                {
                    s.AddSingleton<ITestService>(new TestService($"Value for {t.Value}"));
                });
            });
            var provider = services.BuildServiceProvider();
            var repository = new TenantServiceProviderRepostory(provider);
            var tenant = new TenantKey("tenant1");

            // Act
            var tenantProvider = repository.GetProvider(tenant);
            var testService = tenantProvider.GetService<ITestService>();

            // Assert
            testService.Should().NotBeNull();
            testService.GetValue().Should().Be("Value for tenant1");
        }

        [Fact]
        public void GetProvider_MultipleTenantsWithServices_IsolatesServices()
        {
            // Arrange
            var services = new ServiceCollection();
            services.Configure<TenantAppsCollectionConfigration>(config =>
            {
                config.ForEachTenant((s, t) =>
                {
                    s.AddSingleton<ITestService>(new TestService($"Tenant: {t.Value}"));
                });
            });
            var provider = services.BuildServiceProvider();
            var repository = new TenantServiceProviderRepostory(provider);
            var tenant1 = new TenantKey("tenant1");
            var tenant2 = new TenantKey("tenant2");

            // Act
            var provider1 = repository.GetProvider(tenant1);
            var provider2 = repository.GetProvider(tenant2);
            var service1 = provider1.GetService<ITestService>();
            var service2 = provider2.GetService<ITestService>();

            // Assert
            service1.GetValue().Should().Be("Tenant: tenant1");
            service2.GetValue().Should().Be("Tenant: tenant2");
            service1.Should().NotBeSameAs(service2);
        }

        [Fact]
        public void GetProvider_ConcurrentAccess_ThreadSafe()
        {
            // Arrange
            var services = new ServiceCollection();
            services.Configure<TenantAppsCollectionConfigration>(config => { });
            var provider = services.BuildServiceProvider();
            var repository = new TenantServiceProviderRepostory(provider);
            var tenant = new TenantKey("tenant1");
            var providers = new System.Collections.Concurrent.ConcurrentBag<IServiceProvider>();

            // Act
            Parallel.For(0, 100, i =>
            {
                providers.Add(repository.GetProvider(tenant));
            });

            // Assert
            providers.Distinct().Should().HaveCount(1, "all concurrent calls should return the same cached provider");
        }

        [Fact]
        public void GetProvider_WithLogging_LogsProviderCreation()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<TenantServiceProviderRepostory>>();
            var services = new ServiceCollection();
            services.Configure<TenantAppsCollectionConfigration>(config => { });
            var provider = services.BuildServiceProvider();
            var repository = new TenantServiceProviderRepostory(provider, mockLogger.Object);
            var tenant = new TenantKey("tenant1");

            // Act
            repository.GetProvider(tenant);

            // Assert
            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Creating new service provider for tenant: tenant1")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }
}
