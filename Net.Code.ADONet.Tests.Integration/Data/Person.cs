using System.ComponentModel.DataAnnotations.Schema;

namespace Net.Code.ADONet.Tests.Integration.Data
{
    [MapFromDataRecord]
    [Table("Persoon")]
    public class Person
    {
        public int Id { get; set; }
        [Column("EmailAdres")]
        public string? Email { get; set; }
        public string? Name { get; set; }
        public int RequiredNumber { get; set; }
        public int? OptionalNumber { get; set; }

        public override bool Equals(object? obj)
        {
            if (obj is Person p) return Equals(p);
            return false;
        }
        bool Equals(Person other)
        {
            return other.Id == Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }

    public record Address(int? Id, string? Street, string? ZipCode, string? City, string? Country); 
    
    [MapFromDataRecord]
    public partial record Product(int Id, string Name, decimal Price);

}
