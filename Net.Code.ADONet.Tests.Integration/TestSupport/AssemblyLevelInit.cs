using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Net.Code.ADONet.Tests.Integration.Databases;
using Xunit.Abstractions;
using Xunit.Sdk;

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
                select (IDatabaseImpl)Activator.CreateInstance(t);

            var supportedDbs = q.ToArray();

            foreach (var db in supportedDbs)
            {
                try
                {
                    db.EstablishConnection();
                    db.DropAndRecreate();
                    _available[db.GetType()] = true;
                }
                catch
                {
                    _available[db.GetType()] = false;
                }
            }
        }

        public bool IsAvailable(IDatabaseImpl impl)
        {
            return _available[impl.GetType()];
        }

    }
}