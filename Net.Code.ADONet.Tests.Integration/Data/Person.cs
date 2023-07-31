namespace Net.Code.ADONet.Tests.Integration.Data
{
    [MapFromDataRecord]
    public class Person
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public int RequiredNumber { get; set; }
        public int? OptionalNumber { get; set; }

        public override bool Equals(object obj)
        {
            return Equals(obj as Person);
        }
        bool Equals(Person other)
        {
            return other is not null && other.Id == Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }

    public record Address
    {
        public int Id { get; set; }

        public string Street { get; set; }
        public string ZipCode { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
    }

    public record Product(int Id, string Name, decimal Price);

}