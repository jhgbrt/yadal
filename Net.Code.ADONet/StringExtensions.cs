using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Net.Code.ADONet
{
    public static class StringExtensions
    {
        public static string ToUpperRemoveSpecialChars(this string str) 
            => string.IsNullOrEmpty(str) ? str : Regex.Replace(str, @"([^\w]|_)", "").ToUpperInvariant();
        public static string ToPascalCase(this string str)
        {
            if (string.IsNullOrEmpty(str)) return str;
            var sb = new StringBuilder();
            bool toupper = true;
            foreach (var c in str)
            {
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(toupper ? char.ToUpper(c) : char.ToLower(c));
                    toupper = false;
                }
                else
                {
                    toupper = true;
                }
            }
            return sb.ToString();
        }
        public static string PascalCaseToSentence(this string source) 
            => string.IsNullOrEmpty(source) ? source : string.Join(" ", SplitUpperCase(source));

        public static string ToUpperWithUnderscores(this string source)
        {
            if (string.IsNullOrEmpty(source)) return source;
            return string.Join("_", SplitUpperCase(source).Select(s => s.ToUpperInvariant()));
        }
        public static string ToLowerWithUnderscores(this string source)
        {
            if (string.IsNullOrEmpty(source)) return source;
            return string.Join("_", SplitUpperCase(source).Select(s => s.ToLowerInvariant()));
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