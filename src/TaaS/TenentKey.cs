using System;
using System.Collections.Generic;
using System.Text;

namespace Arfilon.TaaS
{
    public class TenantKey : IEquatable<TenantKey>
    {
        readonly string value;


        public TenantKey(string keyString)
        {
            if (string.IsNullOrWhiteSpace(keyString))
            {
                throw new ArgumentException("Argument not set", nameof(keyString));
            }

            this.value = keyString.ToLower();//Forced format, TenantKey always in lower case

        }

        public string Value => value;
        public override string ToString()
        {
            return "TenantKey:" + value;
        }

        public static TenantKey FromNormalString(string title)
        {
            return new TenantKey(title.Trim().Replace(' ', '_'));
        }

        public static bool operator ==(TenantKey a, TenantKey b)
        {
            return a?.value == b?.value;
        }

        public static bool operator !=(TenantKey a, TenantKey b)
        {
            return a?.value != b?.value;
        }

        public static implicit operator TenantKey(string value)
        {
            return new TenantKey(value);
        }
        public override bool Equals(object obj)
        {
            return value.Equals(obj);
        }
        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        bool IEquatable<TenantKey>.Equals(TenantKey other)
        {
            return this.Value.Equals(other.value);
        }

    }
}
