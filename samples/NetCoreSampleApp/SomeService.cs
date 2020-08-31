using System;
using Net.Code.ADONet;

#pragma warning disable

namespace NetCoreSampleApp
{
    class SomeService : IDisposable
    {
        IDb _db;

        public SomeService(IDb db, SomeDependency dependency)
        {
            Console.WriteLine("SomeService - ctor");
            _db = db;
            _db.Connect();
        }
        public void Dispose()
        {
            Console.WriteLine("SomeService - dispose");
        }
    }



}
