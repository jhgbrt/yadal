using System;
using System.Data;
using System.Reflection;

namespace Net.Code.ADONet
{
    public class MappingConvention
    {
        private readonly Func<IDataRecord, int, string> _getColumnName;
        private readonly Func<PropertyInfo, string> _getPropertyName;

        MappingConvention(
            Func<IDataRecord, int, string> getColumnName, 
            Func<PropertyInfo, string> getPropertyName)
        {
            _getColumnName = getColumnName;
            _getPropertyName = getPropertyName;
        }
        /// <summary>
        /// Maps column names to property names based on exact, case sensitive match
        /// </summary>
        public static readonly MappingConvention Strict = new MappingConvention((record, i) => record.GetName(i), p => p.Name);
        /// <summary>
        /// Maps column names to property names based on case insensitive match, ignoring underscores
        /// </summary>
        public static readonly MappingConvention Loose = new MappingConvention(
            (record, i) => record.GetName(i).ToUpperRemoveSpecialChars(), 
            p => p.Name.ToUpperRemoveSpecialChars()
            );

        public string GetName(IDataRecord record, int i) => _getColumnName(record, i);
        public string GetName(PropertyInfo property) => _getPropertyName(property);
    }
}