using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Net.Code.ADONet.Tests.Unit.StringExtensionsTests
{
    [TestClass]
    public class StringExtensionTests
    {
        [TestMethod]
        public void ToUpperRemoveSpecialChars_NullInput_ReturnsNull()
        {
            string s = null;
            Assert.IsNull(s.ToUpperRemoveSpecialChars());
        }
        [TestMethod]
        public void ToUpperRemoveSpecialChars_EmptyInput_ReturnsNull()
        {
            string s = string.Empty;
            Assert.AreEqual(string.Empty, s.ToUpperRemoveSpecialChars());
        }
        [TestMethod]
        public void ToUpperRemoveSpecialChars_SingleWord_ReturnsWordToUpper()
        {
            string s = "abc";
            Assert.AreEqual("ABC", s.ToUpperRemoveSpecialChars());
        }

        [TestMethod]
        public void ToUpperRemoveSpecialChars_RemovesNonLetterOrDigits()
        {
            string s = "aBc!@#DeF_012[]";
            Assert.AreEqual("ABCDEF012", s.ToUpperRemoveSpecialChars());
        }
        [TestMethod]
        public void ToPascalCase_NullInput_ReturnsNull()
        {
            string s = null;
            Assert.IsNull(s.ToPascalCase());
        }
        [TestMethod]
        public void ToPascalCase_EmptyInput_ReturnsNull()
        {
            string s = string.Empty;
            Assert.AreEqual(string.Empty, s.ToPascalCase());
        }
        [TestMethod]
        public void ToPascalCase_SingleWord_ReturnsWordToUpper()
        {
            string s = "WORD";
            Assert.AreEqual("Word", s.ToPascalCase());
        }
        [TestMethod]
        public void ToPascalCase_SingleLowerWord_ReturnsWordToUpper()
        {
            string s = "word";
            Assert.AreEqual("Word", s.ToPascalCase());
        }

        [TestMethod]
        public void ToPascalCase_RemovesNonLetterOrDigits()
        {
            string s = "SOME_WORD";
            Assert.AreEqual("SomeWord", s.ToPascalCase());
        }
        [TestMethod]
        public void ToPascalCase_LowerCase_RemovesNonLetterOrDigits()
        {
            string s = "some_word";
            Assert.AreEqual("SomeWord", s.ToPascalCase());
        }

        [TestMethod]
        public void PascalCaseToSentence()
        {
            var pascalCase = "SomeSentenceBlah";
            Assert.AreEqual("Some Sentence Blah", pascalCase.PascalCaseToSentence());
        }
        [TestMethod]
        public void PascalCaseToUpperWithUnderscores()
        {
            var pascalCase = "SomeSentenceBlah";
            Assert.AreEqual("SOME_SENTENCE_BLAH", pascalCase.ToUpperWithUnderscores());
        }
        [TestMethod]
        public void PascalCaseToLowerWithUnderscores()
        {
            var pascalCase = "SomeSentenceBlah";
            Assert.AreEqual("some_sentence_blah", pascalCase.ToLowerWithUnderscores());
        }
    }


    public static class ExtensionsUnderDevelopment
    {

    }
}
