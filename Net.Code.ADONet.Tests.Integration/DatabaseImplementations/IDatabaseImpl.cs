using System.Collections.Generic;
using Net.Code.ADONet.Extensions.Experimental;

namespace Net.Code.ADONet.Tests.Integration
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
        string ProviderName { get; }
        bool SupportsTableValuedParameters { get; }
        void DropAndRecreate();
        IDb CreateDb();
        Person Project(dynamic d);
        MultiResultSet<Person, Address> SelectPersonAndAddress(IDb db);
        void BulkInsert(IDb db, IEnumerable<Person> list);
        IQuery Query<T>();
    }
}