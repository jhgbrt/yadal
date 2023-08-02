using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Net.Code.ADONet.SourceGenerators.Tests
{

    public static class ModuleInitializer
    {
        [ModuleInitializer]
        public static void Init()
        {
            VerifySourceGenerators.Initialize();
        }
    }

    [UsesVerify] 
    public class MapFromDataRecordSnapshotTests
    {
        [Theory]
        [InlineData(NamingConvention.PascalCase)]
        [InlineData(NamingConvention.lowercase)]
        [InlineData(NamingConvention.UPPERCASE)]
        public async Task GeneratesMapFromDataRecordExtensionsCorrectly(NamingConvention convention)
        {
            var source = $$"""
                using Net.Code.ADONet;
                namespace My.Namespace;

                [MapFromDataRecord(ColumnNamingConvention = NamingConvention.{{convention}})]
                public class Person
                {
                    public int Id { get; set; }
                    public int? Age { get; set; }
                    public bool IsGood { get; set; }
                    public bool? IsBad { get; set; }
                    public DateTime BirthDate { get; set; }
                    public DateTime? LastSeen { get; set; }
                    public string Name { get; set; }
                    public string? MiddleName { get; set; }
                }
                """;

            // Pass the source code to our helper and snapshot test the output
            var result = TestHelper.Verify(source, convention);
            await result;
        }
    }


    public static class TestHelper
    {
        public static Task Verify<T>(string source, T args)
        {
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);

            var references = AppDomain.CurrentDomain.GetAssemblies()
                                      .Where(assembly => !assembly.IsDynamic)
                                      .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
                                      .Cast<MetadataReference>();

            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName: "SourceGeneratorTests",
                syntaxTrees: new[] { syntaxTree },
                            references,
                            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));


            var generator = new MappingGenerator();

            GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

            driver = driver.RunGeneratorsAndUpdateCompilation(compilation,
                                                              out var outputCompilation,
                                                              out var diagnostics);

            Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

            return Verifier.Verify(driver).UseParameters(args);
        }
    }
}