using System.Text.RegularExpressions;

namespace Net.Code.ADONet;

internal static class StringExtensions
{
    extension(string str)
    {
        public string ToUpperRemoveSpecialChars()
            => string.IsNullOrEmpty(str) ? str : Regex.Replace(str, @"([^\w]|_)", "").ToUpperInvariant();

        public string ToPascalCase() => str?.Aggregate(
            (sb: new StringBuilder(), transform: (Func<char, char>)char.ToUpper),
            (t, c) => char.IsLetterOrDigit(c) ? (t.sb.Append(t.transform(c)), char.ToLower) : (t.sb, char.ToUpper)
            ).sb.ToString() ?? string.Empty;

        public string PascalCaseToSentence()
            => string.IsNullOrEmpty(str) ? str : string.Join(" ", SplitUpperCase(str));

        public string ToUpperWithUnderscores()
            => string.Join("_", SplitUpperCase(str).Select(s => s.ToUpperInvariant()));

        public string ToLowerWithUnderscores()
            => string.Join("_", SplitUpperCase(str).Select(s => s.ToLowerInvariant()));

        public string NoOp()
            => str;
    }

    private static IEnumerable<string> SplitUpperCase(string source)
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
