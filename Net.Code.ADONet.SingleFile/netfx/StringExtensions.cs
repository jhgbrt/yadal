using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Net.Code.ADONet
{
    internal static class StringExtensions
    {
        public static string ToUpperRemoveSpecialChars(this string str) => string.IsNullOrEmpty(str) ? str : Regex.Replace(str, @"([^\w]|_)", "").ToUpperInvariant();
        public static string ToPascalCase(this string str) => str?.Aggregate((sb: new StringBuilder(), transform: (Func<char, char>)char.ToUpper), (t, c) => char.IsLetterOrDigit(c) ? (t.sb.Append(t.transform(c)), char.ToLower) : (t.sb, char.ToUpper)).sb.ToString() ?? string.Empty;
        public static string PascalCaseToSentence(this string source) => string.IsNullOrEmpty(source) ? source : string.Join(" ", SplitUpperCase(source));
        public static string ToUpperWithUnderscores(this string source) => string.Join("_", SplitUpperCase(source).Select(s => s.ToUpperInvariant()));
        public static string ToLowerWithUnderscores(this string source) => string.Join("_", SplitUpperCase(source).Select(s => s.ToLowerInvariant()));
        public static string NoOp(this string source) => source;
        private static IEnumerable<string> SplitUpperCase(string source)
        {
            var wordStart = 0;
            var letters = source.ToCharArray();
            var previous = char.MinValue;
            for (var i = 1; i < letters.Length; i++)
            {
                if (char.IsUpper(letters[i]) && !char.IsWhiteSpace(previous))
                {
                    yield return new string (letters, wordStart, i - wordStart);
                    wordStart = i;
                }

                previous = letters[i];
            }

            yield return new string (letters, wordStart, letters.Length - wordStart);
        }
    }
}
