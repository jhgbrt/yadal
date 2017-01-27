using Xunit;

namespace Net.Code.ADONet.Tests.Unit.StringExtensionsTests
{

    public class StringExtensionTests
    {
        [Fact]
        public void ToUpperRemoveSpecialChars_NullInput_ReturnsNull()
        {
            string s = null;
            Assert.Null(s.ToUpperRemoveSpecialChars());
        }
        [Fact]
        public void ToUpperRemoveSpecialChars_EmptyInput_ReturnsNull()
        {
            string s = string.Empty;
            Assert.Equal(string.Empty, s.ToUpperRemoveSpecialChars());
        }
        [Fact]
        public void ToUpperRemoveSpecialChars_SingleWord_ReturnsWordToUpper()
        {
            string s = "abc";
            Assert.Equal("ABC", s.ToUpperRemoveSpecialChars());
        }

        [Fact]
        public void ToUpperRemoveSpecialChars_RemovesNonLetterOrDigits()
        {
            string s = "aBc!@#DeF_012[]";
            Assert.Equal("ABCDEF012", s.ToUpperRemoveSpecialChars());
        }
        [Fact]
        public void ToPascalCase_NullInput_ReturnsNull()
        {
            string s = null;
            Assert.Null(s.ToPascalCase());
        }
        [Fact]
        public void ToPascalCase_EmptyInput_ReturnsNull()
        {
            string s = string.Empty;
            Assert.Equal(string.Empty, s.ToPascalCase());
        }
        [Fact]
        public void ToPascalCase_SingleWord_ReturnsWordToUpper()
        {
            string s = "WORD";
            Assert.Equal("Word", s.ToPascalCase());
        }
        [Fact]
        public void ToPascalCase_SingleLowerWord_ReturnsWordToUpper()
        {
            string s = "word";
            Assert.Equal("Word", s.ToPascalCase());
        }

        [Fact]
        public void ToPascalCase_RemovesNonLetterOrDigits()
        {
            string s = "SOME_WORD";
            Assert.Equal("SomeWord", s.ToPascalCase());
        }
        [Fact]
        public void ToPascalCase_LowerCase_RemovesNonLetterOrDigits()
        {
            string s = "some_word";
            Assert.Equal("SomeWord", s.ToPascalCase());
        }

        [Fact]
        public void PascalCaseToSentence()
        {
            var pascalCase = "SomeSentenceBlah";
            Assert.Equal("Some Sentence Blah", pascalCase.PascalCaseToSentence());
        }
        [Fact]
        public void PascalCaseToUpperWithUnderscores()
        {
            var pascalCase = "SomeSentenceBlah";
            Assert.Equal("SOME_SENTENCE_BLAH", pascalCase.ToUpperWithUnderscores());
        }
        [Fact]
        public void PascalCaseToLowerWithUnderscores()
        {
            var pascalCase = "SomeSentenceBlah";
            Assert.Equal("some_sentence_blah", pascalCase.ToLowerWithUnderscores());
        }
    }


    public static class ExtensionsUnderDevelopment
    {

    }
}
