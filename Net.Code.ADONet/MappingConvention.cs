using System;

namespace Net.Code.ADONet
{
    public interface IMappingConvention
    {
        string FromDb(string s);
        string ToDb(string s);
        string Parameter(string s);
    }

    internal class MappingConvention : IMappingConvention
    {
        private readonly Func<string, string> _fromDb;
        private readonly Func<string, string> _toDb;
        private readonly char _escape;

        internal MappingConvention(
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
        public static readonly IMappingConvention Default = new MappingConvention(s => s, s => s, '@');
        
        /// <summary>
        /// Maps column names to property names based on case insensitive match, ignoring underscores. Database artefacts are named using
        /// UPPER_CASE_AND_UNDERSCORES
        /// </summary>
        public static readonly IMappingConvention OracleStyle 
            = new MappingConvention(s => s.ToUpperWithUnderscores(), s => s.ToPascalCase(), ':');

        /// <summary>
        /// Maps column names to property names based on case insensitive match, ignoring underscores. Database artefacts are named using
        /// lower_case_and_underscores
        /// </summary>
        public static readonly IMappingConvention UnderScores 
            = new MappingConvention(s => s.ToLowerWithUnderscores(), s => s.ToPascalCase(), '@');

        public string FromDb(string s) => _fromDb(s);
        public string ToDb(string s) => _toDb(s);
        public string Parameter(string s) => $"{_escape}{s}";

    }
}