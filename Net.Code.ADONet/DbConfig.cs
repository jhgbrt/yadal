using System;
using System.Data;
using System.Data.Common;

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
        private static DbConfig Oracle(string providerName) 
            => new DbConfig(
                SetBindByName,
                new MappingConvention(StringExtensions.ToUpperWithUnderscores, StringExtensions.ToPascalCase, ':'), 
                providerName);
        
        private static DbConfig DB2(string providerName) 
            => new DbConfig(
                NoOp, 
                new MappingConvention(StringExtensions.ToUpperWithUnderscores, StringExtensions.ToPascalCase, '@'),
                providerName);
        private static DbConfig PostGreSQL(string providerName) 
            => new DbConfig(
                NoOp, 
                new MappingConvention(StringExtensions.ToLowerWithUnderscores, StringExtensions.ToPascalCase, '@'), 
                providerName);
        private static DbConfig Create(string providerName) 
            => new DbConfig(NoOp, 
                new MappingConvention(StringExtensions.NoOp, StringExtensions.NoOp, '@'), 
                providerName);

        private static void SetBindByName(dynamic c) => c.BindByName = true;
        private static void NoOp(dynamic c) {}

    }
}