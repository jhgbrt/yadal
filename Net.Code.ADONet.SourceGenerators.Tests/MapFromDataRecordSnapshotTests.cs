using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;

namespace Net.Code.ADONet.SourceGenerators.Tests;

public class Person
{
    public int Id { get; set; }
    [Column("TheAge")]
    public int? Age { get; set; }
    public float Float { get; set; }
    public float? NullableFloat { get; set; }
    public double Double { get; set; }
    public double? NullableDouble { get; set; }
    public short Short { get; set; }
    public short? NullableShort { get; set; }
    public long Long { get; set; }
    public long? NullableLong { get; set; }
    public char Char { get; set; }
    public char? NullableChar { get; set; }
    public byte Byte { get; set; }
    public byte? NullableByte { get; set; }
    public Guid MyGuid { get; set; }
    public Guid? NullableGuid { get; set; }
    public bool IsGood { get; set; }
    public bool? IsBad { get; set; }
    public DateTime BirthDate { get; set; }
    public DateTime? LastSeen { get; set; }
    public string Name { get; set; }
    public string? MiddleName { get; set; }
}

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        VerifySourceGenerators.Initialize();
    }
}

public class MapFromDataRecordSnapshotTests
{
    
    [Theory]
    [InlineData(NamingConvention.PascalCase)]
    [InlineData(NamingConvention.lowercase)]
    [InlineData(NamingConvention.UPPERCASE)]
    public async Task GeneratesMapFromDataRecordExtensionsCorrectlyForClass(NamingConvention convention)
    {
        var source = $$"""
            using Net.Code.ADONet;
            using System;
            using System.ComponentModel.DataAnnotations.Schema;
            namespace My.Namespace;

            [MapFromDataRecord(ColumnNamingConvention = NamingConvention.{{convention}})]
            public class Person
            {
                public int Id { get; set; }
                [Column("TheAge")]
                public int? Age { get; set; }
                public float Float { get; set; }
                public float? NullableFloat { get; set; }
                public double Double { get; set; }
                public double? NullableDouble { get; set; }
                public short Short { get; set; }
                public short? NullableShort { get; set; }
                public long Long { get; set; }
                public long? NullableLong { get; set; }
                public char Char { get; set; }
                public char? NullableChar { get; set; }
                public byte Byte { get; set; }
                public byte? NullableByte { get; set; }
                public Guid MyGuid { get; set; }
                public Guid? NullableGuid { get; set; }
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
    [Theory]
    [InlineData(NamingConvention.PascalCase)]
    [InlineData(NamingConvention.lowercase)]
    [InlineData(NamingConvention.UPPERCASE)]
    public async Task GeneratesMapFromDataRecordExtensionsCorrectlyForRecord(NamingConvention convention)
    {
        var source = $$"""
            using Net.Code.ADONet;
            using System;
            using System.ComponentModel.DataAnnotations.Schema;
            namespace My.Namespace;

            [MapFromDataRecord(ColumnNamingConvention = NamingConvention.{{convention}})]
            public record Person(int Id, int? Age, string Name);
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
            .Where(assembly => !assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
            .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
            .Cast<MetadataReference>()
            .Concat([
                MetadataReference.CreateFromFile(typeof(ColumnAttribute).Assembly.Location)
                ])
            .ToArray();

        var compilation = CSharpCompilation.Create(
            assemblyName: "SourceGeneratorTests",
            syntaxTrees: [syntaxTree],
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


