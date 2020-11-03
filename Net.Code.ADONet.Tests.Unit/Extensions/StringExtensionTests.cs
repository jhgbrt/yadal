using Xunit;

namespace Net.Code.ADONet.Tests.Unit.Extensions
{
    public class StringExtensionTests
    {
        [Fact]
        public void ToUpperRemoveSpecialChars_NullInput_ReturnsNull()
        {
            const string s = null;
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
            const string s = "abc";
            Assert.Equal("ABC", s.ToUpperRemoveSpecialChars());
        }

        [Fact]
        public void ToUpperRemoveSpecialChars_RemovesNonLetterOrDigits()
        {
            const string s = "aBc!@#DeF_012[]";
            Assert.Equal("ABCDEF012", s.ToUpperRemoveSpecialChars());
        }
        [Fact]
        public void ToPascalCase_NullInput_ReturnsEmpty()
        {
            const string s = null;
            Assert.Empty(s.ToPascalCase());
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
            const string s = "WORD";
            Assert.Equal("Word", s.ToPascalCase());
        }
        [Fact]
        public void ToPascalCase_SingleLowerWord_ReturnsWordToUpper()
        {
            const string s = "word";
            Assert.Equal("Word", s.ToPascalCase());
        }

        [Fact]
        public void ToPascalCase_RemovesNonLetterOrDigits()
        {
            const string s = "SOME_WORD";
            Assert.Equal("SomeWord", s.ToPascalCase());
        }
        [Fact]
        public void ToPascalCase_LowerCase_RemovesNonLetterOrDigits()
        {
            const string s = "some_word";
            Assert.Equal("SomeWord", s.ToPascalCase());
        }

        [Fact]
        public void PascalCaseToSentence()
        {
            const string pascalCase = "SomeSentenceBlah";
            Assert.Equal("Some Sentence Blah", pascalCase.PascalCaseToSentence());
        }
        [Fact]
        public void PascalCaseToUpperWithUnderscores()
        {
            const string pascalCase = "SomeSentenceBlah";
            Assert.Equal("SOME_SENTENCE_BLAH", pascalCase.ToUpperWithUnderscores());
        }
        [Fact]
        public void PascalCaseToLowerWithUnderscores()
        {
            const string pascalCase = "SomeSentenceBlah";
            Assert.Equal("some_sentence_blah", pascalCase.ToLowerWithUnderscores());
        }
    }

    public static class ExtensionsUnderDevelopment
    {
    }
}
