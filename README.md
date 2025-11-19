# TaaS

TaaS, is a SaaS and Multitenancy Framework for dotNet. This framework aims to provide a tenant engine to help developers build software as a service (SaaS) applications that support multitenancy.

## Features
-	Support for multiple tenants: The framework allows you to easily create and manage multiple tenants within your application. Each tenant can have their own unique data and configuration settings.
-	Tenant isolation: The framework ensures that each tenant's data is isolated from other tenants, ensuring that one tenant's actions do not affect the data of another tenant.
-	Easy integration: The framework is designed to be easy to integrate into your existing C# application. It provides a simple API and a set of helper classes to make it easy to add multitenancy support to your application.
-	Customizable: The framework is highly customizable and can be tailored to fit the needs of your application. You can define custom tenant properties and configure the framework to meet the specific needs of your application.

## Getting Started
To get started with the Taas Framework, you will need to install the framework and set it up in your application.
Installation
You can install the TaaS using the NuGet package manager. Simply run the following command in the Package Manager Console:
>
> Install-Package Arfilon.TaaS.Application
>
Alternatively, you can download the source code from the GitHub repository and build it yourself.

## Quick Start

### 1. Install the Package
```bash
dotnet add package Arfilon.TaaS.Application
```

### 2. Create a Tenant Selector
Implement `ITenantSelector` to define how tenants are identified:

```csharp
public class HostBasedTenantSelector : ITenantSelector
{
    public Task<TenantKey> GetCurrentTenant(HttpContext httpContext)
    {
        var host = httpContext.Request.Host.Host;

        // Extract tenant from subdomain (e.g., tenant1.myapp.com)
        var parts = host.Split('.');
        if (parts.Length > 2)
        {
            var tenantId = parts[0];
            return Task.FromResult(new TenantKey(tenantId));
        }

        throw new TenantNotFoundException(host);
    }
}
```

### 3. Configure Services
In your `Program.cs` or `Startup.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add TaaS services
builder.Services.AddTaaSApplication();

// Register your tenant selector
builder.Services.AddSingleton<ITenantSelector, HostBasedTenantSelector>();

// Configure per-tenant services
builder.Services.AddForEachTenant((services, tenant) =>
{
    // Services registered here are isolated per tenant
    services.AddSingleton<IMyService>(sp => new MyService(tenant.Value));
});

var app = builder.Build();

// Use TaaS middleware
app.UseForEachTenant((tenant, tenantApp) =>
{
    // Configure middleware pipeline per tenant
    tenantApp.UseRouting();
    tenantApp.UseEndpoints(endpoints =>
    {
        endpoints.MapGet("/", async context =>
        {
            var tenantKey = context.RequestServices.GetRequiredService<TenantKey>();
            await context.Response.WriteAsync($"Current Tenant: {tenantKey.Value}");
        });
    });
});

app.Run();
```

## Tenant Selection Strategies

### Subdomain-based
```csharp
public class SubdomainTenantSelector : ITenantSelector
{
    public Task<TenantKey> GetCurrentTenant(HttpContext httpContext)
    {
        var host = httpContext.Request.Host.Host;
        var subdomain = host.Split('.')[0];
        return Task.FromResult(new TenantKey(subdomain));
    }
}
```

### Header-based
```csharp
public class HeaderTenantSelector : ITenantSelector
{
    public Task<TenantKey> GetCurrentTenant(HttpContext httpContext)
    {
        if (httpContext.Request.Headers.TryGetValue("X-Tenant-ID", out var tenantId))
        {
            return Task.FromResult(new TenantKey(tenantId.ToString()));
        }

        throw new TenantNotFoundException("No tenant header found");
    }
}
```

### Path-based
```csharp
public class PathTenantSelector : ITenantSelector
{
    public Task<TenantKey> GetCurrentTenant(HttpContext httpContext)
    {
        var path = httpContext.Request.Path.Value;
        // Extract from path like /tenant1/api/...
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length > 0)
        {
            return Task.FromResult(new TenantKey(segments[0]));
        }

        throw new TenantNotFoundException(path);
    }
}
```

## Advanced Configuration

### Per-Tenant Database Connections
```csharp
builder.Services.AddForEachTenant((services, tenant) =>
{
    var connectionString = $"Server=myserver;Database=Tenant_{tenant.Value};";
    services.AddDbContext<MyDbContext>(options =>
        options.UseSqlServer(connectionString));
});
```

### Per-Tenant Configuration
```csharp
builder.Services.AddForEachTenant((services, tenant) =>
{
    var tenantConfig = configuration.GetSection($"Tenants:{tenant.Value}");
    services.Configure<TenantSettings>(tenantConfig);
});
```

### Custom Tenant Services
```csharp
// Define tenant service provider
public class CustomTenantServiceProvider : ITenantServiceProvider<IMyService>
{
    public IMyService GetService(TenantKey tenant)
    {
        // Custom logic to create service per tenant
        return new MyService(tenant);
    }
}

// Register it
builder.Services.AddTenantServiceProvider<IMyService, CustomTenantServiceProvider>();
```

## Best Practices

### Security
1. **Validate tenant identifiers**: Always validate and sanitize tenant identifiers from requests
2. **Implement authorization**: Ensure users can only access their tenant's data
3. **Use HTTPS**: Always use encrypted connections in production
4. **Rate limiting**: Implement per-tenant rate limiting to prevent abuse

```csharp
public class SecureTenantSelector : ITenantSelector
{
    private readonly ILogger<SecureTenantSelector> _logger;
    private readonly ITenantValidator _validator;

    public async Task<TenantKey> GetCurrentTenant(HttpContext httpContext)
    {
        var tenantId = ExtractTenantId(httpContext);

        // Validate tenant exists and is active
        if (!await _validator.IsValidAsync(tenantId))
        {
            _logger.LogWarning("Invalid tenant access attempt: {TenantId}", tenantId);
            throw new TenantNotFoundException(tenantId);
        }

        return new TenantKey(tenantId);
    }
}
```

### Data Isolation
1. **Database per tenant**: Complete isolation, higher cost
2. **Schema per tenant**: Good isolation, moderate cost
3. **Shared database with tenant column**: Row-level security required

Always implement database-level filtering in addition to TaaS isolation:

```csharp
public class TenantDbContext : DbContext
{
    private readonly TenantKey _tenant;

    public TenantDbContext(DbContextOptions options, TenantKey tenant)
        : base(options)
    {
        _tenant = tenant;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Add global query filter for tenant isolation
        modelBuilder.Entity<MyEntity>()
            .HasQueryFilter(e => e.TenantId == _tenant.Value);
    }
}
```

### Logging
The framework automatically logs tenant resolution, service provider creation, and errors. Configure logging in your application:

```csharp
builder.Logging.AddFilter("Arfilon.TaaS", LogLevel.Information);
```

### Testing
```csharp
[Fact]
public async Task TestTenantIsolation()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddTaaSApplication();
    services.AddSingleton<ITenantSelector>(new TestTenantSelector());

    // Act & Assert
    // Test your tenant-specific logic
}
```

## Troubleshooting

### TenantNotFoundException
**Cause**: Tenant selector returned null or tenant doesn't exist
**Solution**: Check your tenant selector implementation and ensure the tenant identifier is valid

### Service Not Registered
**Cause**: Service not registered in per-tenant container
**Solution**: Use `AddForEachTenant()` to register services that should be tenant-specific

### Multiple Tenants Getting Same Data
**Cause**: Database queries not filtered by tenant
**Solution**: Implement query filters or use separate databases per tenant

## Documentation

### Core Concepts
- **TenantKey**: Immutable identifier for a tenant (case-insensitive)
- **ITenantSelector**: Interface for determining current tenant from HTTP context
- **ITenantServiceProvider<T>**: Custom service factory for tenant-specific services
- **AddTaaSApplication()**: Registers framework services
- **UseForEachTenant()**: Middleware for per-tenant request processing
- **AddForEachTenant()**: Registers services in per-tenant containers

### Architecture
TaaS creates a separate DI container for each tenant. When a request arrives:
1. `ITenantSelector` determines the tenant from the HTTP context
2. Framework retrieves or creates the tenant's DI container
3. Request is processed using tenant-specific services
4. Services are isolated - tenant A cannot access tenant B's services

### Performance Considerations
- **Memory**: Each tenant has its own IServiceProvider (plan for ~1-10MB per tenant)
- **Caching**: Tenant providers are cached after first creation
- **Concurrency**: Thread-safe concurrent tenant initialization
- **Scope**: New scope created per request for tenant-specific services

## Contributions
We welcome contributions to TaaS Framework If you have a bug fix or a new feature that you would like to contribute, please open a pull request on the GitHub repository.

