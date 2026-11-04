using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Reflection;
using System.Runtime.Loader;

namespace Net.Code.ADONet.SourceGenerators.Tests;

/// <summary>
/// Runs the generator, compiles the generated mapping code into an in-memory assembly
/// and exercises the generated To&lt;Type&gt; extension against real data readers.
/// </summary>
public class MapFromDataRecordRuntimeTests
{
    const string ClassSource = """
        using Net.Code.ADONet;
        namespace My.Namespace;

        [MapFromDataRecord]
        public class Person
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string? MiddleName { get; set; }
        }
        """;

    const string RecordSource = """
        using Net.Code.ADONet;
        namespace My.Namespace;

        [MapFromDataRecord]
        public record Person(int Id, string Name, string? MiddleName);
        """;

    public static TheoryData<string> Kinds => new() { "class", "record" };

    static string SourceFor(string kind) => kind == "record" ? RecordSource : ClassSource;

    [Theory]
    [MemberData(nameof(Kinds))]
    public void MapsCorrectly_WhenSameTypeIsReadFromQueriesWithDifferentColumnOrder(string kind)
    {
        var map = GeneratedMapper.Create(SourceFor(kind), "My.Namespace.PersonMapper", "ToPerson");

        using (var reader1 = CreateReader(("Id", typeof(int)), ("Name", typeof(string)), ("MiddleName", typeof(string)))
            .With(1, "Alice", "A."))
        {
            Assert.True(reader1.Read());
            var p1 = map(reader1);
            Assert.Equal(1, Get(p1, "Id"));
            Assert.Equal("Alice", Get(p1, "Name"));
            Assert.Equal("A.", Get(p1, "MiddleName"));
        }

        using (var reader2 = CreateReader(("MiddleName", typeof(string)), ("Name", typeof(string)), ("Id", typeof(int)))
            .With("B.", "Bob", 2))
        {
            Assert.True(reader2.Read());
            var p2 = map(reader2);
            Assert.Equal(2, Get(p2, "Id"));
            Assert.Equal("Bob", Get(p2, "Name"));
            Assert.Equal("B.", Get(p2, "MiddleName"));
        }
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void MapsDbNullToNull_ForNullableReferenceTypeProperty(string kind)
    {
        var map = GeneratedMapper.Create(SourceFor(kind), "My.Namespace.PersonMapper", "ToPerson");

        using var reader = CreateReader(("Id", typeof(int)), ("Name", typeof(string)), ("MiddleName", typeof(string)))
            .With(1, "Alice", DBNull.Value);

        Assert.True(reader.Read());
        var p = map(reader);
        Assert.Equal(1, Get(p, "Id"));
        Assert.Equal("Alice", Get(p, "Name"));
        Assert.Null(Get(p, "MiddleName"));
    }

    const string DateTimeClassSource = """
        using System;
        using Net.Code.ADONet;
        namespace My.Namespace;

        [MapFromDataRecord]
        public class Shift
        {
            public DateOnly Day { get; set; }
            public DateOnly? EndDay { get; set; }
            public TimeOnly Start { get; set; }
            public TimeOnly? End { get; set; }
        }
        """;

    const string DateTimeRecordSource = """
        using System;
        using Net.Code.ADONet;
        namespace My.Namespace;

        [MapFromDataRecord]
        public record Shift(DateOnly Day, DateOnly? EndDay, TimeOnly Start, TimeOnly? End);
        """;

    static string DateTimeSourceFor(string kind) => kind == "record" ? DateTimeRecordSource : DateTimeClassSource;

    static readonly DateOnly Day = new(2024, 5, 6);
    static readonly TimeOnly Start = new(8, 30);

    // Npgsql 10+ reports 'date' and 'time' columns as DateOnly / TimeOnly
    [Theory]
    [MemberData(nameof(Kinds))]
    public void MapsDateOnlyAndTimeOnly_FromDateOnlyAndTimeOnlyColumns(string kind)
    {
        var map = GeneratedMapper.Create(DateTimeSourceFor(kind), "My.Namespace.ShiftMapper", "ToShift");

        using var reader = CreateReader(("Day", typeof(DateOnly)), ("EndDay", typeof(DateOnly)), ("Start", typeof(TimeOnly)), ("End", typeof(TimeOnly)))
            .With(Day, DBNull.Value, Start, new TimeOnly(17, 0));

        Assert.True(reader.Read());
        var s = map(reader);
        Assert.Equal(Day, Get(s, "Day"));
        Assert.Null(Get(s, "EndDay"));
        Assert.Equal(Start, Get(s, "Start"));
        Assert.Equal(new TimeOnly(17, 0), Get(s, "End"));
    }

    // SqlClient, Sqlite, MySql, ... report 'date' and 'time' columns as DateTime / TimeSpan
    [Theory]
    [MemberData(nameof(Kinds))]
    public void MapsDateOnlyAndTimeOnly_FromDateTimeAndTimeSpanColumns(string kind)
    {
        var map = GeneratedMapper.Create(DateTimeSourceFor(kind), "My.Namespace.ShiftMapper", "ToShift");

        using var reader = CreateReader(("Day", typeof(DateTime)), ("EndDay", typeof(DateTime)), ("Start", typeof(TimeSpan)), ("End", typeof(TimeSpan)))
            .With(new DateTime(2024, 5, 6), new DateTime(2024, 5, 7), new TimeSpan(8, 30, 0), DBNull.Value);

        Assert.True(reader.Read());
        var s = map(reader);
        Assert.Equal(Day, Get(s, "Day"));
        Assert.Equal(new DateOnly(2024, 5, 7), Get(s, "EndDay"));
        Assert.Equal(Start, Get(s, "Start"));
        Assert.Null(Get(s, "End"));
    }

    static object? Get(object o, string property) => o.GetType().GetProperty(property)!.GetValue(o);

    static ReaderBuilder CreateReader(params (string name, Type type)[] columns)
    {
        var table = new DataTable();
        foreach (var (name, type) in columns)
            table.Columns.Add(name, type);
        return new ReaderBuilder(table);
    }

    sealed class ReaderBuilder(DataTable table)
    {
        public DataTableReader With(params object[] values)
        {
            table.Rows.Add(values);
            return table.CreateDataReader();
        }
    }

    static class GeneratedMapper
    {
        public static Func<IDataRecord, object> Create(string source, string mapperTypeName, string methodName)
        {
            var parseOptions = new CSharpParseOptions(LanguageVersion.Latest);
            var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);

            var trustedAssemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
                .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

            var references = trustedAssemblies
                .Concat([
                    typeof(MapFromDataRecordAttribute).Assembly.Location,
                    typeof(ColumnAttribute).Assembly.Location,
                ])
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
                .ToArray();

            var compilation = CSharpCompilation.Create(
                assemblyName: "GeneratedMapperRuntimeTests_" + Guid.NewGuid().ToString("N"),
                syntaxTrees: [syntaxTree],
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

            GeneratorDriver driver = CSharpGeneratorDriver.Create([new MappingGenerator().AsSourceGenerator()], parseOptions: parseOptions);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generatorDiagnostics);
            Assert.Empty(generatorDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

            using var stream = new MemoryStream();
            var emitResult = outputCompilation.Emit(stream);
            Assert.True(emitResult.Success, string.Join(Environment.NewLine, emitResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)));

            stream.Position = 0;
            var context = new AssemblyLoadContext(compilation.AssemblyName, isCollectible: true);
            var assembly = context.LoadFromStream(stream);
            var method = assembly.GetType(mapperTypeName, throwOnError: true)!
                .GetMethod(methodName, BindingFlags.Public | BindingFlags.Static)!;

            return record =>
            {
                try
                {
                    return method.Invoke(null, [record])!;
                }
                catch (TargetInvocationException e) when (e.InnerException is not null)
                {
                    System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e.InnerException).Throw();
                    throw;
                }
            };
        }
    }
}
