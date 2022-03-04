using System.Collections.Generic;
using System.Linq;

namespace Net.Code.ADONet.Tests.Integration.Data
{
    public static class FakeData
    {
        public static class Id
        {
            private static int _current;

            public static int Next()
            {
                return ++_current;
            }
        }

        public static class People
        {
            public static IEnumerable<Person> List(int n) => Enumerable.Range(1, n).Select(x => One());

            public static Person One()
            {
                var fullName = Faker.Name.FullName();
                int? optionalNumber = Faker.RandomNumber.Next(short.MaxValue);
                return new Person
                {
                    Id = Id.Next(),
                    Name = fullName,
                    Email = Faker.Internet.Email(fullName),
                    RequiredNumber = Faker.RandomNumber.Next(short.MaxValue),
                    OptionalNumber = optionalNumber % 2 == 0 ? optionalNumber : null
                };
            }
        }

        public static class Addresses
        {
            public static Address[] List(int n) => Enumerable.Range(1, n).Select(x => One()).ToArray();
            public static Address One() => new()
            {
                Id = Id.Next(),
                Street = Faker.Address.StreetAddress(),
                ZipCode = Faker.Address.ZipCode(),
                City = Faker.Address.City(),
                Country = Faker.Address.Country()
            };
        }

        public static class Products
        {
            public static Product[] List(int n) => Enumerable.Range(1, n).Select(x => One()).ToArray();

            public static Product One() => new (Id.Next(), Faker.Company.Name(), Faker.RandomNumber.Next() + .25m);
        }
    }
}