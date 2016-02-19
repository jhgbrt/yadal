using System;
using System.Collections.Generic;
using System.Linq;

namespace Net.Code.ADONet
{
    public class MappingConvention
    {
        private readonly Func<string, string> _fromDb;
        private readonly Func<string, string> _toDb;
        private readonly char _escape;

        MappingConvention(
            Func<string, string> todb,
            Func<string, string> fromdb,
            char escape)
        {
            _toDb = todb;
            _fromDb = fromdb;
            _escape = escape;
        }
        /// <summary>
        /// Maps column names to property names based on exact, case sensitive match
        /// </summary>
        public static readonly MappingConvention Default = new MappingConvention(
            s => s, 
            s => s, '@');
        /// <summary>
        /// Maps column names to property names based on case insensitive match, ignoring underscores
        /// </summary>
        public static readonly MappingConvention OracleStyle = new MappingConvention(
            s => s.ToPascalCase(), 
            s => s.ToUpperWithUnderscores(), 
            ':'
            );

        public string FromDb(string s) => _toDb(s);
        public string ToDb(string s) => _fromDb(s);

        public string JoinAsColumnNames(IEnumerable<string> propertyNames) => string.Join(",", propertyNames.Select(ToDb));
        public string JoinAsVariableNames(IEnumerable<string> propertyNames) => string.Join(",", propertyNames.Select(s => $"{_escape}{s}"));
    }
}