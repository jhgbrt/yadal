using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Net.Code.ADONet.Tests.Unit.StringExtensionsTests
{
    [TestFixture]
    public class StringExtensionTests
    {
        [Test]
        public void ToUpperRemoveSpecialChars_NullInput_ReturnsNull()
        {
            string s = null;
            Assert.IsNull(s.ToUpperRemoveSpecialChars());
        }
        [Test]
        public void ToUpperRemoveSpecialChars_EmptyInput_ReturnsNull()
        {
            string s = string.Empty;
            Assert.AreEqual(string.Empty, s.ToUpperRemoveSpecialChars());
        }
        [Test]
        public void ToUpperRemoveSpecialChars_SingleWord_ReturnsWordToUpper()
        {
            string s = "abc";
            Assert.AreEqual("ABC", s.ToUpperRemoveSpecialChars());
        }

        [Test]
        public void ToUpperRemoveSpecialChars_RemovesNonLetterOrDigits()
        {
            string s = "aBc!@#DeF_012[]";
            Assert.AreEqual("ABCDEF012", s.ToUpperRemoveSpecialChars());
        }
        [Test]
        public void ToPascalCase_NullInput_ReturnsNull()
        {
            string s = null;
            Assert.IsNull(s.ToPascalCase());
        }
        [Test]
        public void ToPascalCase_EmptyInput_ReturnsNull()
        {
            string s = string.Empty;
            Assert.AreEqual(string.Empty, s.ToPascalCase());
        }
        [Test]
        public void ToPascalCase_SingleWord_ReturnsWordToUpper()
        {
            string s = "WORD";
            Assert.AreEqual("Word", s.ToPascalCase());
        }

        [Test]
        public void ToPascalCase_RemovesNonLetterOrDigits()
        {
            string s = "SOME_WORD";
            Assert.AreEqual("SomeWord", s.ToPascalCase());
        }

        [Test]
        public void PascalCaseToSentence()
        {
            var pascalCase = "SomeSentenceBlah";
            Assert.AreEqual("Some Sentence Blah", pascalCase.PascalCaseToSentence());
        }
        [Test]
        public void PascalCaseToUpperWithUnderscores()
        {
            var pascalCase = "SomeSentenceBlah";
            Assert.AreEqual("SOME_SENTENCE_BLAH", pascalCase.PascalCaseToUpperWithUnderscores());
        }
    }


    public static class ExtensionsUnderDevelopment
    {
        public static string ToPascalCase(this string str)
            =>
                string.IsNullOrEmpty(str)
                    ? str
                    : CultureInfo.InvariantCulture.TextInfo.ToTitleCase(str.ToLower()).Replace("_", "");

        public static string PascalCaseToSentence(this string source)
        {
            if (string.IsNullOrEmpty(source)) return source;
            return string.Join(" ", SplitUpperCase(source));
        }
        public static string PascalCaseToUpperWithUnderscores(this string source)
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
