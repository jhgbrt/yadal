using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Net.Code.ADONet.Tests.Integration
{
    [TestClass]
    public class AssemblyLevelInit
    {
        [AssemblyInitialize]
        public static void AssemblyInit(TestContext c)
        {
            var q =
                from t in Assembly.GetExecutingAssembly().GetTypes()
                where typeof (IDatabaseImpl).IsAssignableFrom(t)
                      && !t.IsInterface && !t.IsAbstract
                select Activator.CreateInstance(t);

            var supportedDbs = q.OfType<IDatabaseImpl>().ToArray();

            foreach (var db in supportedDbs)
                try
                {
                    db.DropAndRecreate();
                }
                catch (Exception e)
                {
                    Trace.TraceError(e.ToString());
                }
        }

    }
}