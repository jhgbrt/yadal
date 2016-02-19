using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Net.Code.ADONet
{
    public static class StringExtensions
    {
        public static string ToUpperRemoveSpecialChars(this string str) 
            => string.IsNullOrEmpty(str) ? str : Regex.Replace(str, @"([^\w]|_)", "").ToUpperInvariant();
        public static string ToPascalCase(this string str)
            => string.IsNullOrEmpty(str)
                    ? str
                    : CultureInfo.InvariantCulture.TextInfo.ToTitleCase(str.ToLower()).Replace("_", "");

        public static string PascalCaseToSentence(this string source) 
            => string.IsNullOrEmpty(source) ? source : string.Join(" ", SplitUpperCase(source));

        public static string ToUpperWithUnderscores(this string source)
        {
            if (string.IsNullOrEmpty(source)) return source;
            return string.Join("_", SplitUpperCase(source).Select(s => s.ToUpperInvariant()));
        }

        static IEnumerable<string> SplitUpperCase(string source)
        {
            var wordStart = 0;
            var letters = source.ToCharArray();
            var previous = char.MinValue;
            for (var i = 1; i < letters.Length; i++)
            {
                if (char.IsUpper(letters[i]) && !char.IsWhiteSpace(previous))
                {
                    yield return new string(letters, wordStart, i - wordStart);
                    wordStart = i;
                }
                previous = letters[i];
            }
            yield return new string(letters, wordStart, letters.Length - wordStart);
        }
    }
}