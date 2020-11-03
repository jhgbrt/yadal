using Net.Code.ADONet.Extensions.Mapping;
using System;
using System.Data;
using System.Data.Common;

namespace Net.Code.ADONet
{
    /// <summary>
    /// <para>
    /// The DbConfig class allows to configure database specific behaviour at runtime, without a direct
    /// dependency on the underlying ADO.Net provider. It does 2 things
    /// </para>
    /// <para>
    /// - provides a hook to configure a DbCommand in case some specific configuration is required. For example,
    ///   Oracle requires the BindByName property to be set to true for named parameters to work.
    /// - Sets the way database naming conventions are mapped to .Net naming conventions. For example, in Oracle,
    ///   database and column names are upper case and separated by underscores. Postgres defaults to lower case.
    ///   This includes also the escape character that indicates parameter names in queries with named parameters.
    /// </para>
    /// </summary>
    public class DbConfig
    {
        internal Action<IDbCommand> PrepareCommand { get; }
        internal IMappingConvention MappingConvention { get; }
        public DbConfig(Action<IDbCommand> prepareCommand, IMappingConvention? mappingConvention = null)
        {
            PrepareCommand = prepareCommand;
            MappingConvention = mappingConvention ?? Extensions.Mapping.MappingConvention.Default;
        }
        public static DbConfig FromProviderName(string providerName) => providerName switch
        {
            string s when s.StartsWith("Oracle") => Oracle,
            string s when s.StartsWith("Npgsql") => PostGreSQL,
            string s when s.StartsWith("IBM") => DB2,
            _ => Default
        };

        public static DbConfig FromProviderFactory(DbProviderFactory factory)
            => FromProviderName(factory.GetType().FullName);

        // By default, the Oracle driver does not support binding parameters by name;
        // one has to set the BindByName property on the OracleDbCommand.
        // Mapping: 
        // Oracle convention is to work with UPPERCASE_AND_UNDERSCORE instead of BookTitleCase
        public static readonly DbConfig Oracle
            = new DbConfig(
                SetBindByName,
                new MappingConvention(StringExtensions.ToUpperWithUnderscores, StringExtensions.ToPascalCase, ':')
                );
        public static readonly DbConfig DB2
            = new DbConfig(
                NoOp,
                new MappingConvention(StringExtensions.ToUpperWithUnderscores, StringExtensions.ToPascalCase, '@')
                );
        public static readonly DbConfig PostGreSQL
            = new DbConfig(
                NoOp,
                new MappingConvention(StringExtensions.ToLowerWithUnderscores, StringExtensions.ToPascalCase, '@')
                );
        public static readonly DbConfig Default
            = new DbConfig(
                NoOp,
                new MappingConvention(StringExtensions.NoOp, StringExtensions.NoOp, '@')
                );

        private static void SetBindByName(dynamic c) => c.BindByName = true;
        private static void NoOp(dynamic c) {}
    }
}