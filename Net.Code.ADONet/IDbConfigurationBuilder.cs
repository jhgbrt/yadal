using System;
using System.Data;

namespace Net.Code.ADONet
{
    public interface IDbConfigurationBuilder
    {
        /// <summary>
        /// Provides a hook to configure an ADO.Net DbCommmand just after it is created. 
        /// For example, the Oracle.DataAccess API requires the BindByName property to be set
        /// to true for the datareader to enable named access to the result columns (Note that 
        /// for this situation you don't need to do anything, it's handled by default).
        /// </summary>
        /// <param name="action"></param>
        IDbConfigurationBuilder OnPrepareCommand(Action<IDbCommand> action);
        /// <summary>
        /// Set the mapping convention used to map property names and db column names
        /// </summary>
        /// <param name="convention"></param>
        IDbConfigurationBuilder WithMappingConvention(MappingConvention convention);
    }
}