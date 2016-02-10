using System;
using System.Data;

namespace Net.Code.ADONet
{
    public class DbConfig
    {
        private static readonly Action<IDbCommand> Empty = c => { };

        private static readonly MappingConvention Default = MappingConvention.Strict;

        public DbConfig()
            : this(Empty, Default)
        {
        }

        public DbConfig(Action<IDbCommand> prepareCommand, MappingConvention convention)
        {
            PrepareCommand = prepareCommand;
            MappingConvention = convention;
        }

        public Action<IDbCommand> PrepareCommand { get; internal set; }
        public MappingConvention MappingConvention { get; internal set; }
    }
}