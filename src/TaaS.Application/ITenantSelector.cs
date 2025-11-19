using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Arfilon.TaaS
{
    /// <summary>
    /// Defines a mechanism for determining the current tenant from an HTTP request.
    /// </summary>
    /// <remarks>
    /// Implementations of this interface define the strategy for extracting tenant information
    /// from incoming HTTP requests. Common strategies include:
    /// <list type="bullet">
    /// <item><description>Subdomain-based: Extract tenant from request host (e.g., tenant1.app.com)</description></item>
    /// <item><description>Header-based: Extract tenant from HTTP header (e.g., X-Tenant-ID)</description></item>
    /// <item><description>Path-based: Extract tenant from URL path (e.g., /tenant1/api/...)</description></item>
    /// <item><description>Cookie-based: Extract tenant from authentication cookie claims</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class SubdomainTenantSelector : ITenantSelector
    /// {
    ///     public Task&lt;TenantKey&gt; GetCurrentTenant(HttpContext httpContext)
    ///     {
    ///         var host = httpContext.Request.Host.Host;
    ///         var subdomain = host.Split('.')[0];
    ///         return Task.FromResult(new TenantKey(subdomain));
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface ITenantSelector
    {
        /// <summary>
        /// Determines the current tenant from the provided HTTP context.
        /// </summary>
        /// <param name="httpContext">The HTTP context of the current request.</param>
        /// <returns>
        /// A <see cref="Task{TenantKey}"/> representing the asynchronous operation.
        /// Returns the identified <see cref="TenantKey"/>, or <c>null</c> if no tenant could be determined.
        /// </returns>
        /// <remarks>
        /// Returning <c>null</c> will result in a <see cref="TenantNotFoundException"/> being thrown by the middleware.
        /// Implementations should validate tenant identifiers and may throw <see cref="TenantNotFoundException"/> directly
        /// if the tenant is invalid or inactive.
        /// </remarks>
        /// <exception cref="TenantNotFoundException">
        /// Thrown when the tenant cannot be determined or is invalid.
        /// </exception>
        Task<TenantKey> GetCurrentTenant(Microsoft.AspNetCore.Http.HttpContext httpContext);
    }
}
