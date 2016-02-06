using System;

namespace Net.Code.ADONet.Tests.Integration
{
    public class Person
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public int RequiredNumber { get; set; }
        public int? OptionalNumber { get; set; }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Person) obj);
        }

        protected bool Equals(Person other)
        {
            return Id == other.Id 
                && string.Equals(Email, other.Email) 
                && string.Equals(Name, other.Name) 
                && RequiredNumber == other.RequiredNumber 
                && OptionalNumber == other.OptionalNumber;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Id;
                hashCode = (hashCode*397) ^ (Email?.GetHashCode() ?? 0);
                hashCode = (hashCode*397) ^ (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ RequiredNumber;
                hashCode = (hashCode*397) ^ OptionalNumber.GetHashCode();
                return hashCode;
            }
        }
    }
}