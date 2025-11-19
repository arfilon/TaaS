# Security Guidelines for TaaS Multi-Tenant Applications

## Overview
Multi-tenant applications require careful security considerations to prevent data leakage between tenants, ensure proper authorization, and protect against attacks. This document provides security best practices when using the TaaS framework.

## Critical Security Requirements

### 1. Tenant Validation and Sanitization

**ALWAYS validate tenant identifiers** before using them:

```csharp
public class SecureTenantSelector : ITenantSelector
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ILogger<SecureTenantSelector> _logger;

    public async Task<TenantKey> GetCurrentTenant(HttpContext httpContext)
    {
        var rawTenantId = ExtractTenantId(httpContext);

        // Sanitize input
        if (string.IsNullOrWhiteSpace(rawTenantId))
        {
            _logger.LogWarning("Empty tenant identifier in request from {IP}",
                httpContext.Connection.RemoteIpAddress);
            throw new TenantNotFoundException("empty");
        }

        // Validate tenant exists and is active
        var tenant = await _tenantRepository.GetByIdAsync(rawTenantId);
        if (tenant == null || !tenant.IsActive)
        {
            _logger.LogWarning("Invalid or inactive tenant access attempt: {TenantId} from {IP}",
                rawTenantId, httpContext.Connection.RemoteIpAddress);
            throw new TenantNotFoundException(rawTenantId);
        }

        return new TenantKey(tenant.Id);
    }
}
```

### 2. Database-Level Tenant Isolation

**Framework isolation is NOT sufficient** - always implement database-level filtering:

```csharp
public class TenantDbContext : DbContext
{
    private readonly TenantKey _tenant;

    public TenantDbContext(DbContextOptions<TenantDbContext> options, TenantKey tenant)
        : base(options)
    {
        _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Global query filter for ALL tenant-specific entities
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, "TenantId");
                var tenantValue = Expression.Constant(_tenant.Value);
                var filter = Expression.Lambda(
                    Expression.Equal(property, tenantValue),
                    parameter);

                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
            }
        }
    }

    public override int SaveChanges()
    {
        // Ensure tenant ID is set on all entities
        SetTenantId();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetTenantId();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void SetTenantId()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added && e.Entity is ITenantEntity);

        foreach (var entry in entries)
        {
            ((ITenantEntity)entry.Entity).TenantId = _tenant.Value;
        }
    }
}

public interface ITenantEntity
{
    string TenantId { get; set; }
}
```

### 3. User Authentication and Authorization

**Verify users belong to the tenant** they're accessing:

```csharp
public class TenantAuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantAuthorizationMiddleware> _logger;

    public TenantAuthorizationMiddleware(RequestDelegate next, ILogger<TenantAuthorizationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userTenantId = context.User.FindFirst("TenantId")?.Value;
            var currentTenant = context.RequestServices.GetRequiredService<TenantKey>();

            if (userTenantId != currentTenant.Value)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to access tenant {RequestedTenant} but belongs to {UserTenant}",
                    context.User.FindFirst("sub")?.Value,
                    currentTenant.Value,
                    userTenantId);

                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("Access denied");
                return;
            }
        }

        await _next(context);
    }
}
```

### 4. Input Validation

**Prevent injection attacks** in tenant identifiers:

```csharp
public static class TenantValidation
{
    private static readonly Regex ValidTenantIdPattern = new Regex(@"^[a-z0-9_-]+$", RegexOptions.Compiled);

    public static bool IsValidTenantId(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            return false;

        if (tenantId.Length > 50)
            return false;

        return ValidTenantIdPattern.IsMatch(tenantId);
    }
}

public class ValidatingTenantSelector : ITenantSelector
{
    public async Task<TenantKey> GetCurrentTenant(HttpContext httpContext)
    {
        var rawTenantId = ExtractTenantId(httpContext);

        if (!TenantValidation.IsValidTenantId(rawTenantId))
        {
            throw new SecurityException($"Invalid tenant identifier format: {rawTenantId}");
        }

        return new TenantKey(rawTenantId);
    }
}
```

### 5. Audit Logging

**Log all tenant access** for security auditing:

```csharp
public class AuditingTenantSelector : ITenantSelector
{
    private readonly ITenantSelector _innerSelector;
    private readonly IAuditLogger _auditLogger;

    public async Task<TenantKey> GetCurrentTenant(HttpContext httpContext)
    {
        var tenant = await _innerSelector.GetCurrentTenant(httpContext);

        await _auditLogger.LogAsync(new AuditEvent
        {
            TenantId = tenant.Value,
            UserId = httpContext.User.FindFirst("sub")?.Value,
            Action = "TenantAccess",
            IPAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
            Timestamp = DateTime.UtcNow,
            Path = httpContext.Request.Path,
            Method = httpContext.Request.Method
        });

        return tenant;
    }
}
```

## Attack Prevention

### SQL Injection
- Use parameterized queries ALWAYS
- Implement query filters at DbContext level
- Never concatenate tenant IDs into SQL strings

```csharp
// WRONG - Vulnerable to SQL injection
var sql = $"SELECT * FROM Orders WHERE TenantId = '{tenant.Value}'";

// CORRECT - Parameterized query
var orders = await context.Orders
    .Where(o => o.TenantId == tenant.Value)
    .ToListAsync();
```

### Tenant Enumeration
- Don't expose tenant existence in error messages
- Use same error message for "tenant not found" and "access denied"
- Implement rate limiting on tenant resolution

```csharp
public class RateLimitedTenantSelector : ITenantSelector
{
    private readonly IDistributedCache _cache;

    public async Task<TenantKey> GetCurrentTenant(HttpContext httpContext)
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString();
        var cacheKey = $"tenant_attempts:{ip}";
        var attempts = await _cache.GetStringAsync(cacheKey);

        if (int.TryParse(attempts, out var count) && count > 10)
        {
            throw new TooManyRequestsException("Rate limit exceeded");
        }

        // Increment attempts
        await _cache.SetStringAsync(cacheKey, (count + 1).ToString(),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1) });

        // Proceed with tenant resolution
        return await ResolveTenantAsync(httpContext);
    }
}
```

### Denial of Service (DoS)
- Implement tenant provider cache limits
- Set maximum number of concurrent tenants
- Monitor memory usage

```csharp
public class LimitedTenantServiceProviderRepository : TenantServiceProviderRepostory
{
    private const int MaxTenants = 1000;
    private readonly ConcurrentDictionary<string, IServiceProvider> _providers;

    public override IServiceProvider GetProvider(TenantKey tenant)
    {
        if (_providers.Count >= MaxTenants)
        {
            throw new InvalidOperationException(
                $"Maximum tenant limit ({MaxTenants}) reached. Contact support.");
        }

        return base.GetProvider(tenant);
    }
}
```

### Cross-Tenant Data Leakage
- Test tenant isolation rigorously
- Use integration tests to verify isolation
- Implement automated security scanning

```csharp
[Fact]
public async Task VerifyTenantDataIsolation()
{
    // Create data for tenant1
    var tenant1Context = CreateContextForTenant("tenant1");
    tenant1Context.Orders.Add(new Order { Id = 1, TenantId = "tenant1" });
    await tenant1Context.SaveChangesAsync();

    // Try to access from tenant2
    var tenant2Context = CreateContextForTenant("tenant2");
    var orders = await tenant2Context.Orders.ToListAsync();

    // Verify tenant2 cannot see tenant1's data
    Assert.Empty(orders);
}
```

## Configuration Security

### Secrets Management
Never store tenant-specific secrets in code:

```csharp
builder.Services.AddForEachTenant((services, tenant) =>
{
    // WRONG - Hardcoded connection string
    // services.AddDbContext<MyDbContext>(options =>
    //     options.UseSqlServer($"Server=...;Database={tenant.Value};Password=hardcoded"));

    // CORRECT - Use Azure Key Vault or similar
    var connectionString = keyVaultClient.GetSecretAsync(
        $"https://myvault.vault.azure.net/secrets/tenant-{tenant.Value}-connection-string").Result.Value;

    services.AddDbContext<MyDbContext>(options =>
        options.UseSqlServer(connectionString));
});
```

### HTTPS Enforcement
Always use HTTPS in production:

```csharp
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
    options.Preload = true;
});

app.UseHttpsRedirection();
app.UseHsts();
```

## Security Checklist

- [ ] Tenant identifiers are validated and sanitized
- [ ] Database queries include tenant filters (query filters or separate DBs)
- [ ] User authorization checks tenant membership
- [ ] All tenant access is audit logged
- [ ] Rate limiting is implemented
- [ ] HTTPS is enforced
- [ ] Secrets are stored securely (Key Vault, environment variables)
- [ ] Integration tests verify tenant isolation
- [ ] SQL injection prevention (parameterized queries)
- [ ] Error messages don't leak tenant information
- [ ] Maximum tenant limits are enforced
- [ ] Monitoring and alerting for suspicious tenant access
- [ ] Regular security audits and penetration testing
- [ ] Dependency vulnerabilities are scanned
- [ ] Backup and recovery procedures are tenant-aware

## Reporting Security Issues

If you discover a security vulnerability in the TaaS framework, please report it privately to the maintainers rather than creating a public issue. This allows us to address the vulnerability before it can be exploited.

## Additional Resources

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [OWASP Multi-Tenant Security Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Multitenant_Architecture_Cheat_Sheet.html)
- [Microsoft: Multi-tenant SaaS database tenancy patterns](https://docs.microsoft.com/en-us/azure/sql-database/saas-tenancy-app-design-patterns)
