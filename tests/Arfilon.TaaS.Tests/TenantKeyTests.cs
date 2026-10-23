using System;
using Arfilon.TaaS;
using FluentAssertions;
using Xunit;

namespace Arfilon.TaaS.Tests
{
    public class TenantKeyTests
    {
        [Fact]
        public void Constructor_WithValidKey_CreatesInstance()
        {
            // Arrange
            var keyString = "TestTenant";

            // Act
            var tenantKey = new TenantKey(keyString);

            // Assert
            tenantKey.Should().NotBeNull();
            tenantKey.Value.Should().Be(keyString.ToLower());
        }

        [Fact]
        public void Constructor_WithMixedCaseKey_ConvertsToLowerCase()
        {
            // Arrange
            var keyString = "TeSt-TeNaNt";

            // Act
            var tenantKey = new TenantKey(keyString);

            // Assert
            tenantKey.Value.Should().Be("test-tenant");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_WithNullOrWhitespace_ThrowsArgumentException(string invalidKey)
        {
            // Act
            Action act = () => new TenantKey(invalidKey);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithParameterName("keyString");
        }

        [Fact]
        public void FromNormalString_WithValidTitle_CreatesInstance()
        {
            // Arrange
            var title = "Test Tenant Name";

            // Act
            var tenantKey = TenantKey.FromNormalString(title);

            // Assert
            tenantKey.Value.Should().Be("test_tenant_name");
        }

        [Fact]
        public void FromNormalString_WithLeadingTrailingSpaces_TrimsAndConverts()
        {
            // Arrange
            var title = "  Test Tenant  ";

            // Act
            var tenantKey = TenantKey.FromNormalString(title);

            // Assert
            tenantKey.Value.Should().Be("test_tenant");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void FromNormalString_WithNullOrWhitespace_ThrowsArgumentException(string invalidTitle)
        {
            // Act
            Action act = () => TenantKey.FromNormalString(invalidTitle);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithParameterName("title");
        }

        [Fact]
        public void EqualsOperator_WithSameValue_ReturnsTrue()
        {
            // Arrange
            var key1 = new TenantKey("tenant1");
            var key2 = new TenantKey("TENANT1");

            // Act & Assert
            (key1 == key2).Should().BeTrue();
        }

        [Fact]
        public void EqualsOperator_WithDifferentValue_ReturnsFalse()
        {
            // Arrange
            var key1 = new TenantKey("tenant1");
            var key2 = new TenantKey("tenant2");

            // Act & Assert
            (key1 == key2).Should().BeFalse();
        }

        [Fact]
        public void EqualsOperator_WithNullValues_HandlesCorrectly()
        {
            // Arrange
            TenantKey key1 = null;
            TenantKey key2 = null;
            var key3 = new TenantKey("tenant");

            // Act & Assert
            (key1 == key2).Should().BeTrue();
            (key1 == key3).Should().BeFalse();
            (key3 == key1).Should().BeFalse();
        }

        [Fact]
        public void NotEqualsOperator_WithDifferentValue_ReturnsTrue()
        {
            // Arrange
            var key1 = new TenantKey("tenant1");
            var key2 = new TenantKey("tenant2");

            // Act & Assert
            (key1 != key2).Should().BeTrue();
        }

        [Fact]
        public void NotEqualsOperator_WithSameValue_ReturnsFalse()
        {
            // Arrange
            var key1 = new TenantKey("tenant1");
            var key2 = new TenantKey("TENANT1");

            // Act & Assert
            (key1 != key2).Should().BeFalse();
        }

        [Fact]
        public void Equals_WithTenantKey_ComparesCorrectly()
        {
            // Arrange
            var key1 = new TenantKey("tenant1");
            var key2 = new TenantKey("TENANT1");
            var key3 = new TenantKey("tenant2");

            // Act & Assert
            key1.Equals(key2).Should().BeTrue();
            key1.Equals(key3).Should().BeFalse();
        }

        [Fact]
        public void Equals_WithString_ComparesCorrectly()
        {
            // Arrange
            var key = new TenantKey("tenant1");

            // Act & Assert
            key.Equals("tenant1").Should().BeTrue();
            key.Equals("TENANT1").Should().BeTrue();
            key.Equals("tenant2").Should().BeFalse();
        }

        [Fact]
        public void Equals_WithNull_ReturnsFalse()
        {
            // Arrange
            var key = new TenantKey("tenant1");

            // Act & Assert
            key.Equals(null).Should().BeFalse();
        }

        [Fact]
        public void Equals_WithDifferentType_ReturnsFalse()
        {
            // Arrange
            var key = new TenantKey("tenant1");
            var obj = new object();

            // Act & Assert
            key.Equals(obj).Should().BeFalse();
        }

        [Fact]
        public void Equals_WithSameReference_ReturnsTrue()
        {
            // Arrange
            var key = new TenantKey("tenant1");

            // Act & Assert
            key.Equals(key).Should().BeTrue();
        }

        [Fact]
        public void GetHashCode_WithSameValue_ReturnsSameHash()
        {
            // Arrange
            var key1 = new TenantKey("tenant1");
            var key2 = new TenantKey("TENANT1");

            // Act
            var hash1 = key1.GetHashCode();
            var hash2 = key2.GetHashCode();

            // Assert
            hash1.Should().Be(hash2);
        }

        [Fact]
        public void GetHashCode_WithDifferentValue_ReturnsDifferentHash()
        {
            // Arrange
            var key1 = new TenantKey("tenant1");
            var key2 = new TenantKey("tenant2");

            // Act
            var hash1 = key1.GetHashCode();
            var hash2 = key2.GetHashCode();

            // Assert
            hash1.Should().NotBe(hash2);
        }

        [Fact]
        public void ImplicitStringConversion_CreatesValidTenantKey()
        {
            // Arrange
            string keyString = "testtenant";

            // Act
            TenantKey tenantKey = keyString;

            // Assert
            tenantKey.Should().NotBeNull();
            tenantKey.Value.Should().Be("testtenant");
        }

        [Fact]
        public void ToString_ReturnsFormattedString()
        {
            // Arrange
            var key = new TenantKey("tenant1");

            // Act
            var result = key.ToString();

            // Assert
            result.Should().Be("TenantKey:tenant1");
        }

        [Fact]
        public void IEquatable_Equals_WithNull_ReturnsFalse()
        {
            // Arrange
            var key = new TenantKey("tenant1");
            IEquatable<TenantKey> equatable = key;

            // Act & Assert
            equatable.Equals(null).Should().BeFalse();
        }

        [Fact]
        public void IEquatable_Equals_WithSameValue_ReturnsTrue()
        {
            // Arrange
            var key1 = new TenantKey("tenant1");
            var key2 = new TenantKey("TENANT1");
            IEquatable<TenantKey> equatable = key1;

            // Act & Assert
            equatable.Equals(key2).Should().BeTrue();
        }
    }
}
