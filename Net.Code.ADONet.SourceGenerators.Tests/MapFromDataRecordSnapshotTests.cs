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
        [Fact]
        public async Task GeneratesMapFromDataRecordExtensionsCorrectly()
        {
            // The source code to test
            var source = """
                using Net.Code.ADONet;
                namespace My.Namespace;

                [MapFromDataRecord]
                public class Person
                {
                    public int Id { get; set; }
                    public int? Age { get; set; }
                    public string Name { get; set; }
                    public string? MiddleName { get; set; }
                }
                """;

            // Pass the source code to our helper and snapshot test the output
            var result = TestHelper.Verify(source);
            await result;
        }
    }


    public static class TestHelper
    {
        public static Task Verify(string source)
        {
            var config = new DbConfig(c => { }, MappingConvention.Default);

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

            return Verifier.Verify(driver);
        }
    }
}