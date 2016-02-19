namespace Net.Code.ADONet.Tests.Integration
{
    public interface IDatabaseImpl
    {
        string CreatePersonTable { get; }
        string CreateAddressTable { get; }
        string InsertPerson { get; }
        string InsertAddress { get; }
        bool SupportsMultipleResultSets { get; }
        string ProviderName { get; }
        bool SupportsTableValuedParameters { get; }
        string EscapeChar { get; }
        void DropAndRecreate();
        IDb CreateDb();
        Person Project(dynamic d);
        MultiResultSet<Person, Address> SelectPersonAndAddress(IDb db);
        void BulkInsert(IDb db, Person[] list);
    }
}