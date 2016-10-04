using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;

namespace Net.Code.ADONet
{
    public class DbConfig
    {
        internal DbConfig(Action<IDbCommand> prepareCommand, IMappingConvention convention, string providerName)
        {
            PrepareCommand = prepareCommand;
            MappingConvention = convention;
            ProviderName = providerName;
        }

        public Action<IDbCommand> PrepareCommand { get; }
        internal IMappingConvention MappingConvention { get; }
        public string ProviderName { get; }

        public static readonly DbConfig Default = Create("System.Data.SqlClient");

        public static DbConfig FromProviderName(string providerName)
        {
            return !string.IsNullOrEmpty(providerName) && providerName.StartsWith("Oracle") ? Oracle(providerName) : Create(providerName);
        }

        // By default, the Oracle driver does not support binding parameters by name;
        // one has to set the BindByName property on the OracleDbCommand.
        // Mapping: 
        // Oracle convention is to work with UPPERCASE_AND_UNDERSCORE instead of BookTitleCase
        private static DbConfig Oracle(string providerName) => new DbConfig(SetBindByName, Net.Code.ADONet.MappingConvention.OracleStyle, providerName);

        private static DbConfig Create(string providerName) => new DbConfig(c => { }, Net.Code.ADONet.MappingConvention.Default, providerName);

        private static void SetBindByName(dynamic c) => c.BindByName = true;

    }
}