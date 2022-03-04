namespace Net.Code.ADONet.Tests.Integration.Data
{
    public record Person
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public int RequiredNumber { get; set; }
        public int? OptionalNumber { get; set; }
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