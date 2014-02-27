using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Net.Code.ADONet.Tests.Sqlite
{
    public class MyObject
    {
        public override bool Equals(object obj)
        {
            return Equals((MyObject)obj);
        }

        protected bool Equals(MyObject other)
        {
            return Id == other.Id
                   && string.Equals(StringNotNull, other.StringNotNull)
                   && string.Equals(StringNull, other.StringNull)
                   && NullableUniqueId.Equals(other.NullableUniqueId)
                   && NonNullableUniqueId.Equals(other.NonNullableUniqueId)
                   && NullableInt == other.NullableInt
                   && NonNullableInt == other.NonNullableInt;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Id;
                hashCode = (hashCode*397) ^ (StringNotNull != null ? StringNotNull.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (StringNull != null ? StringNull.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ NullableUniqueId.GetHashCode();
                hashCode = (hashCode*397) ^ NonNullableUniqueId.GetHashCode();
                hashCode = (hashCode*397) ^ NullableInt.GetHashCode();
                hashCode = (hashCode*397) ^ NonNullableInt;
                return hashCode;
            }
        }

        public static bool operator ==(MyObject left, MyObject right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (ReferenceEquals(left, null)) return false;
            return left.Equals(right);
        }

        public static bool operator !=(MyObject left, MyObject right)
        {
            return !(left == right);
        }

        public int Id { get; set; }
        public string StringNotNull { get; set; }
        public string StringNull { get; set; }
        public Guid? NullableUniqueId { get; set; }
        public Guid NonNullableUniqueId { get; set; }
        public int? NullableInt { get; set; }
        public int NonNullableInt { get; set; }

        public override string ToString()
        {
            return
                string.Format(
                    "Id = {0}, StringNotNull = {1}, StringNull = {2}, NullableUniqueId = {3}, NonNullableUniqueId = {4}, NullableInt = {5}, NonNullableInt = {6}",
                    Id, StringNotNull, StringNull, NullableUniqueId, NonNullableUniqueId, NullableInt, NonNullableInt);
        }

    }
}