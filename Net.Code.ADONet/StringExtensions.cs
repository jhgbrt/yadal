using System.Text.RegularExpressions;

namespace Net.Code.ADONet
{
    public static class StringExtensions
    {
        public static string ToUpperRemoveSpecialChars(this string str) 
            => string.IsNullOrEmpty(str) ? str : Regex.Replace(str, @"([^\w]|_)", "").ToUpperInvariant();
    }
}