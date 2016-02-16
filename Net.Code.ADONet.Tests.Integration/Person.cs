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

    public class Address
    {
        public int Id { get; set; }

        public string Street { get; set; }
        public string ZipCode { get; set; }
        public string City { get; set; }
        public string Country { get; set; }

        public override bool Equals(object o) {
            if (ReferenceEquals(null, o)) return false;
            if (ReferenceEquals(this, o)) return true;
            if (o.GetType() != this.GetType()) return false;
            return Equals((Address) o);
        }

        protected bool Equals(Address other)
        {
            return Id == other.Id && string.Equals(Street, other.Street) && string.Equals(ZipCode, other.ZipCode) && string.Equals(City, other.City) && string.Equals(Country, other.Country);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Id;
                hashCode = (hashCode*397) ^ (Street != null ? Street.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (ZipCode != null ? ZipCode.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (City != null ? City.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (Country != null ? Country.GetHashCode() : 0);
                return hashCode;
            }
        }
    }



}