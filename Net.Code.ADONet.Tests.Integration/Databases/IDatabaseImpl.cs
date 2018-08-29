using System.Collections.Generic;
using Net.Code.ADONet.Extensions.Experimental;
using Net.Code.ADONet.Tests.Integration.Data;

namespace Net.Code.ADONet.Tests.Integration.Databases
{
    public interface IDatabaseImpl
    {
        bool EstablishConnection();
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