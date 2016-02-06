using System;
using System.Configuration;
using System.Data.SqlServerCe;
using System.IO;

namespace Net.Code.ADONet.Tests.Integration
{
    public class SqlServerCe : BaseDb
    {
        public override bool SupportsMultipleResultSets => false;
        public override void DropAndRecreate()
        {
            var masterConnectionString = ConfigurationManager.ConnectionStrings[MasterName].ConnectionString;
            var connectionStringBuilder = new SqlCeConnectionStringBuilder
            {
                ConnectionString = masterConnectionString
            };
            string fileName = connectionStringBuilder.DataSource;
            if (File.Exists(fileName)) File.Delete(fileName);
            var en = new SqlCeEngine(masterConnectionString);
            en.CreateDatabase();
        }

        protected override Type ProviderType => typeof (SqlCeProviderFactory);
    }
}