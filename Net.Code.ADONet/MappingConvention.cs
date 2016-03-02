using System;

namespace Net.Code.ADONet
{
    internal class MappingConvention
    {
        private readonly Func<string, string> _fromDb;
        private readonly Func<string, string> _toDb;
        private readonly char _escape;

        public MappingConvention(
            Func<string, string> todb,
            Func<string, string> fromdb,
            char escape)
        {
            _toDb = todb;
            _fromDb = fromdb;
            _escape = escape;
        }

        /// <summary>
        /// Maps column names to property names based on exact, case sensitive match. Database artefacts are named exactly
        /// like the .Net objects.
        /// </summary>
        public static readonly MappingConvention Default = new MappingConvention(s => s, s => s, '@');
        
        /// <summary>
        /// Maps column names to property names based on case insensitive match, ignoring underscores. Database artefacts are named using
        /// UPPER_CASE_AND_UNDERSCORES
        /// </summary>
        public static readonly MappingConvention OracleStyle = new MappingConvention(s => s.ToPascalCase(), s => s.ToUpperWithUnderscores(), ':');

        /// <summary>
        /// Maps column names to property names based on case insensitive match, ignoring underscores. Database artefacts are named using
        /// lower_case_and_underscores
        /// </summary>
        public static readonly MappingConvention UnderScores = new MappingConvention(s => s.ToPascalCase(), s => s.ToLowerWithUnderscores(), '@');

        public string FromDb(string s) => _toDb(s);
        public string ToDb(string s) => _fromDb(s);
        public string Parameter(string s) => $"{_escape}{s}";

        public MappingConvention WithEscapeCharacter(char e)
        {
            return new MappingConvention(_toDb, _fromDb, e);
        }
        public MappingConvention MapPropertyToDbName(Func<string, string> todb)
        {
            return new MappingConvention(todb, _fromDb, _escape);
        }
        public MappingConvention MapDbNameToPropertyName(Func<string, string> fromdb)
        {
            return new MappingConvention(_toDb, fromdb, _escape);
        }
    }
}