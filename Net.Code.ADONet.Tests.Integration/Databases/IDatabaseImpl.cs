using System.Collections.Generic;

using Net.Code.ADONet.Tests.Integration.Data;

namespace Net.Code.ADONet.Tests.Integration.Databases
{
    public interface IDatabaseImpl
    {
        bool IsAvailable();
        string CreatePersonTable { get; }
        string DropPersonTable { get; }
        string CreateAddressTable { get; }
        string DropAddressTable { get; }
        string InsertPerson { get; }
        bool SupportsMultipleResultSets { get; }
        bool SupportsTableValuedParameters { get; }
        DbConfig Config { get; }

        void DropAndRecreate();
        IDb CreateDb();
        Person Project(dynamic d);
        (IReadOnlyCollection<Person>, IReadOnlyCollection<Address>) SelectPersonAndAddress(IDb db);
        void BulkInsert(IDb db, IEnumerable<Person> list);
        Query CreateQuery<T>();
    }
}