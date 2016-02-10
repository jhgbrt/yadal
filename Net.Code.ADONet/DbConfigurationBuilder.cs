using System;
using System.Data;
using System.Reflection;

namespace Net.Code.ADONet
{
    class DbConfigurationBuilder : IDbConfigurationBuilder
    {
        public DbConfig Config { get; } 

        internal DbConfigurationBuilder(DbConfig config)
        {
            Config = config;
        }

        internal DbConfigurationBuilder() : this(new DbConfig()) { }

        public IDbConfigurationBuilder OnPrepareCommand(Action<IDbCommand> action)
        {
            Config.PrepareCommand = action;
            return this;
        }

        public IDbConfigurationBuilder WithMappingConvention(MappingConvention convention)
        {
            Config.MappingConvention = convention;
            return this;
        }

        class Option<T>
        {
            public bool HasValue { get; private set; }
            public T Value { get; private set; }

            public void SetValue(T v)
            {
                Value = v;
                HasValue = true;
            }
        }
        private static readonly Option<PropertyInfo> BindByName = new Option<PropertyInfo>();
        private DbConfigurationBuilder Oracle()
        {
            // By default, the Oracle driver does not support binding parameters by name;
            // one has to set the BindByName property on the OracleDbCommand.
            // Since we don't want to have a hard reference to Oracle.DataAccess here,
            // we use reflection.
            // The day Oracle decides to make a breaking change, this will blow up with 
            // a runtime exception
            OnPrepareCommand(command =>
            {
                if (!BindByName.HasValue)
                    BindByName.SetValue(command.GetType().GetProperty("BindByName"));
                BindByName.Value?.SetValue(command, true, null);
            });

            // Oracle convention is to work with UPPERCASE_AND_UNDERSCORE instead of BookTitleCase
            WithMappingConvention(MappingConvention.Loose);

            return this;
        }
        public DbConfigurationBuilder FromProviderName(string providerName)
        {
            switch (providerName)
            {
                case "Oracle.DataAccess.Client":
                case "Oracle.ManagedDataAccess.Client":
                    return Oracle();
            }
            return this;
        }
    }
}