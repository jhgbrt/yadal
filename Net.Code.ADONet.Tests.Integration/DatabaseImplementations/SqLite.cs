using System;
using System.Configuration;
using System.Data.SQLite;
using System.IO;

namespace Net.Code.ADONet.Tests.Integration
{
    public class SqLite : BaseDb
    {
        public override void DropAndRecreate()
        {
            var masterConnectionString = ConfigurationManager.ConnectionStrings[MasterName].ConnectionString;
            var connectionStringBuilder = new SQLiteConnectionStringBuilder
            {
                ConnectionString = masterConnectionString
            };
            string fileName = connectionStringBuilder.DataSource;
            if (File.Exists(fileName)) File.Delete(fileName);
        }

        protected override Type ProviderType => typeof (SQLiteFactory);
    }
}