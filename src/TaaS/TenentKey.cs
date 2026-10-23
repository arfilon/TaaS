using System;
using System.Collections.Generic;
using System.Text;

namespace Arfilon.TaaS
{
    /// <summary>
    /// Represents an immutable, case-insensitive identifier for a tenant in a multi-tenant application.
    /// </summary>
    /// <remarks>
    /// TenantKey is designed as a value object that ensures consistent tenant identification across the application.
    /// All comparisons are case-insensitive, and the internal value is always stored in lowercase.
    /// </remarks>
    public class TenantKey : IEquatable<TenantKey>
    {
        readonly string value;

        /// <summary>
        /// Initializes a new instance of the <see cref="TenantKey"/> class.
        /// </summary>
        /// <param name="keyString">The tenant identifier string. Will be converted to lowercase.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="keyString"/> is null, empty, or whitespace.</exception>
        public TenantKey(string keyString)
        {
            if (string.IsNullOrWhiteSpace(keyString))
            {
                throw new ArgumentException("Argument not set", nameof(keyString));
            }

            this.value = keyString.ToLower();//Forced format, TenantKey always in lower case

        }

        /// <summary>
        /// Gets the lowercase string value of the tenant identifier.
        /// </summary>
        public string Value => value;

        /// <summary>
        /// Returns a string representation of this tenant key.
        /// </summary>
        /// <returns>A string in the format "TenantKey:{value}".</returns>
        public override string ToString()
        {
            return "TenantKey:" + value;
        }

        /// <summary>
        /// Creates a new <see cref="TenantKey"/> from a human-readable string by trimming whitespace and replacing spaces with underscores.
        /// </summary>
        /// <param name="title">The human-readable title to convert. Example: "My Company" becomes "my_company".</param>
        /// <returns>A new <see cref="TenantKey"/> instance.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="title"/> is null, empty, or whitespace.</exception>
        /// <example>
        /// <code>
        /// var tenantKey = TenantKey.FromNormalString("My Company Name");
        /// // Result: tenantKey.Value == "my_company_name"
        /// </code>
        /// </example>
        public static TenantKey FromNormalString(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("Title cannot be null or whitespace", nameof(title));
            }
            return new TenantKey(title.Trim().Replace(' ', '_'));
        }

        /// <summary>
        /// Determines whether two <see cref="TenantKey"/> instances are equal (case-insensitive).
        /// </summary>
        /// <param name="a">The first tenant key to compare.</param>
        /// <param name="b">The second tenant key to compare.</param>
        /// <returns><c>true</c> if the instances are equal; otherwise, <c>false</c>. Handles null values correctly.</returns>
        public static bool operator ==(TenantKey a, TenantKey b)
        {
            return a?.value == b?.value;
        }

        /// <summary>
        /// Determines whether two <see cref="TenantKey"/> instances are not equal (case-insensitive).
        /// </summary>
        /// <param name="a">The first tenant key to compare.</param>
        /// <param name="b">The second tenant key to compare.</param>
        /// <returns><c>true</c> if the instances are not equal; otherwise, <c>false</c>. Handles null values correctly.</returns>
        public static bool operator !=(TenantKey a, TenantKey b)
        {
            return a?.value != b?.value;
        }

        /// <summary>
        /// Implicitly converts a string to a <see cref="TenantKey"/>.
        /// </summary>
        /// <param name="value">The string value to convert.</param>
        /// <returns>A new <see cref="TenantKey"/> instance.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is null, empty, or whitespace.</exception>
        public static implicit operator TenantKey(string value)
        {
            return new TenantKey(value);
        }
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj is TenantKey other)
                return value.Equals(other.value);
            if (obj is string str)
                return value.Equals(str.ToLower());
            return false;
        }
        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        bool IEquatable<TenantKey>.Equals(TenantKey other)
        {
            if (other == null) return false;
            return this.value.Equals(other.value);
        }

    }
}
