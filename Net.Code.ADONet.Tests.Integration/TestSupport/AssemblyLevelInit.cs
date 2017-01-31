using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Net.Code.ADONet.Tests.Integration.Databases;

namespace Net.Code.ADONet.Tests.Integration.TestSupport
{


    public class AssemblyLevelInit
    {
        private IDictionary<Type, bool> _available = new Dictionary<Type, bool>();
        public AssemblyLevelInit()
        {
            var q =
                from t in Assembly.GetExecutingAssembly().GetTypes()
                where typeof (IDatabaseImpl).IsAssignableFrom(t)
                      && !t.IsInterface && !t.IsAbstract
                select Activator.CreateInstance(t);

            var supportedDbs = q.OfType<IDatabaseImpl>().ToArray();

            foreach (var db in supportedDbs)
            {
                var isAvailable = db.IsAvailable();
                _available[db.GetType()] = isAvailable;
                if (isAvailable)
                    db.DropAndRecreate();
            }
        }

        public bool IsAvailable(IDatabaseImpl impl)
        {
            return _available[impl.GetType()];
        }

    }
}