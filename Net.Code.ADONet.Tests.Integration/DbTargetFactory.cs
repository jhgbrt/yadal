using System;
using System.Linq;
using System.Reflection;

namespace Net.Code.ADONet.Tests.Integration
{
    public static class DbTargetFactory
    {
        public static BaseDb Create(string name)
        {
            var targetType = (
                from t in Assembly.GetExecutingAssembly().GetTypes()
                where typeof(BaseDb).IsAssignableFrom(t) && t.Name == name
                select t
                ).Single();

            return (BaseDb)Activator.CreateInstance(targetType);
        }
    }
}