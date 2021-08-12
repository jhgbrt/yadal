using Net.Code.ADONet;

#pragma warning disable

namespace NetCoreSampleApp;

public class SomeDependency
{
    IDb _db;
    public SomeDependency(IDb db)
    {
        Console.WriteLine("SomeDependency - ctor");
        _db = db;
        Console.WriteLine(db.Connection.State);
    }
}
