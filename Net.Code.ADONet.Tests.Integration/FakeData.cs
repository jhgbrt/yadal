using System.Linq;

namespace Net.Code.ADONet.Tests.Integration
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
            public static Person[] List(int n)
            {
                return Enumerable.Range(1, n).Select(x => One()).ToArray();

            }

            public static Person One()
            {
                var fullName = Faker.Name.FullName();
                return new Person
                {
                    Id = Id.Next(),
                    Name = fullName,
                    Email = Faker.Internet.Email(fullName),
                    RequiredNumber = Faker.RandomNumber.Next(short.MaxValue),
                    OptionalNumber = Faker.RandomNumber.Next(short.MaxValue)
                };
            }
        }

    }
}