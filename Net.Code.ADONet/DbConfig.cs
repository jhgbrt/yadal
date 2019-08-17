using System;
using System.Data;
using System.Data.Common;

namespace Net.Code.ADONet
{
    /// <summary>
    /// The DbConfig class allows to configure database specific behaviour at runtime, without a direct 
    /// dependency on the underlying ADO.Net provider. It does 2 things
    /// 
    /// - provides a hook to configure a DbCommand in case some specific configuration is required. For example,
    ///   Oracle requires the BindByName property to be set to true for named parameters to work.
    /// - Sets the way database naming conventions are mapped to .Net naming conventions. For example, in Oracle, 
    ///   database and column names are upper case and separated by underscores. Postgres defaults to lower case.
    ///   This includes also the escape character that indicates parameter names in queries with named parameters.
    /// </summary>
    public class DbConfig
    {
        public DbConfig(Action<IDbCommand> prepareCommand, IMappingConvention convention, string providerName)
        {
            PrepareCommand = prepareCommand;
            MappingConvention = convention;
            ProviderName = providerName;
        }

        public Action<IDbCommand> PrepareCommand { get; }
        internal IMappingConvention MappingConvention { get; }
        public string ProviderName { get; }

        public static readonly DbConfig Default = Create(string.Empty);

        public static DbConfig FromProviderName(string providerName)
        {
            if (!string.IsNullOrEmpty(providerName) && providerName.StartsWith("Oracle"))
                return Oracle(providerName);
            if (!string.IsNullOrEmpty(providerName) && providerName.StartsWith("Npgsql"))
                return PostGreSQL(providerName);
            if (!string.IsNullOrEmpty(providerName) && providerName.StartsWith("IBM"))
                return DB2(providerName);
            return Create(providerName);
        }
        public static DbConfig FromProviderFactory(DbProviderFactory factory) 
        {
            return FromProviderName(factory.GetType().FullName);
        }

        // By default, the Oracle driver does not support binding parameters by name;
        // one has to set the BindByName property on the OracleDbCommand.
        // Mapping: 
        // Oracle convention is to work with UPPERCASE_AND_UNDERSCORE instead of BookTitleCase
        public static DbConfig Oracle(string providerName) 
            => new DbConfig(
                SetBindByName,
                new MappingConvention(StringExtensions.ToUpperWithUnderscores, StringExtensions.ToPascalCase, ':'), 
                providerName);

        public static DbConfig DB2(string providerName) 
            => new DbConfig(
                NoOp, 
                new MappingConvention(StringExtensions.ToUpperWithUnderscores, StringExtensions.ToPascalCase, '@'),
                providerName);
        public static DbConfig PostGreSQL(string providerName) 
            => new DbConfig(
                NoOp, 
                new MappingConvention(StringExtensions.ToLowerWithUnderscores, StringExtensions.ToPascalCase, '@'), 
                providerName);
        public static DbConfig Create(string providerName) 
            => new DbConfig(NoOp, 
                new MappingConvention(StringExtensions.NoOp, StringExtensions.NoOp, '@'), 
                providerName);
        public static DbConfig Create(Action<IDbCommand> prepareCommand,
            MappingConvention mappingConvention,
            string providerName)
            => new DbConfig(prepareCommand,
                mappingConvention,
                providerName);

        private static void SetBindByName(dynamic c) => c.BindByName = true;
        private static void NoOp(dynamic c) {}

    }
}