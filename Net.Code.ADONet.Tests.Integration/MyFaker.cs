using System;
using System.Linq;

namespace Net.Code.ADONet.Tests.Integration
{
    public static class MyFaker
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
                return Enumerable.Range(1, n).Select(id =>
                {
                    var fullName = Faker.Name.FullName();
                    return new Person
                    {
                        Id = id,
                        Name = fullName,
                        Email = Faker.Internet.Email(fullName),
                        RequiredNumber = Faker.RandomNumber.Next(),
                        OptionalNumber = Faker.RandomNumber.Next(),
                        //UniqueId = Guid.NewGuid()
                    };
                }).ToArray();

            }
        }

    }
}