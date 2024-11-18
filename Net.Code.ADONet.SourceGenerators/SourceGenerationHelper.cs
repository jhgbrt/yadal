﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Net.Code.ADONet.SourceGenerators
{
    public static class SourceGenerationHelper
    {
        internal static SourceCode GenerateExtensionClass(MapperInfo mapper)
        {
            using var builder = new SourceBuilder()
                .WriteLine("// <auto-generated>")
                .WriteUsings("System.Data")
                .WriteLine()
                .WriteLine(mapper.Namespace is null ? "" : $"namespace {mapper.Namespace};")
                .WriteLine($"public static partial class {mapper.Name}Mapper")
                .WriteOpeningBracket();


            builder.WriteLine($"static class Ordinal")
                .WriteOpeningBracket()
                .WriteLine("public static bool __initialized__;");

            foreach (var p in mapper.Properties)
            {
                builder.WriteLine($"public static int {p.Name};");
            }

            builder.WriteClosingBracket();

            builder.WriteLine($"public static {mapper.Name} To{mapper.Name}(this IDataRecord record)")
                .WriteOpeningBracket()
                .WriteLine("if (!Ordinal.__initialized__)")
                .WriteOpeningBracket();
            foreach (var p in mapper.Properties)
            {
                builder.WriteLine($"Ordinal.{p.Name} = record.GetOrdinal(\"{p.ColumnName().Transform(mapper.NamingConvention)}\");");
            }
            builder
            .WriteLine("System.Threading.Thread.MemoryBarrier();")
            .WriteLine($"Ordinal.__initialized__ = true;")
            .WriteClosingBracket();

            if (mapper.IsRecord)
            {
                builder.Write($"return new {mapper.Name} (");

                builder.Write(string.Join(",",
                    mapper.Properties.Select(p => p switch
                    {
                        { Nullable: true, IsValueType: true } => $"record.IsDBNull(Ordinal.{p.Name}) ? null : record.{GetGetMethod(p)}(Ordinal.{p.Name})",
                        _ => $"record.{GetGetMethod(p)}(Ordinal.{p.Name})"
                    })
                    ));

                builder.WriteLine(");");

            }
            else
            {
                builder.WriteLine($"return new {mapper.Name}")
                .WriteOpeningBracket();

                foreach (var p in mapper.Properties)
                {
                    if (p.Nullable && p.IsValueType)
                    {
                        builder.WriteLine($"{p.Name} = record.IsDBNull(Ordinal.{p.Name}) ? null : record.{GetGetMethod(p)}(Ordinal.{p.Name}),");
                    }
                    else
                    {
                        builder.WriteLine($"{p.Name} = record.{GetGetMethod(p)}(Ordinal.{p.Name}),");
                    }
                }
                builder.WriteClosingBracket(true);
            }

            builder.WriteClosingBracket()
                .WriteClosingBracket();

            return new(builder.ToString(), $"{mapper.Namespace??"global"}.{mapper.Name}MapFromDataRecordExtension.g.cs");

        }

        private static string GetGetMethod(PropertyInfo property)
        {
            return $"{property.Type switch
            {
                "Boolean" => nameof(IDataRecord.GetBoolean),
                "Byte" => nameof(IDataRecord.GetByte),
                "Char" => nameof(IDataRecord.GetChar),
                "DateTime" => nameof(IDataRecord.GetDateTime),
                "Decimal" => nameof(IDataRecord.GetDecimal),
                "Double" => nameof(IDataRecord.GetDouble),
                "Guid" => nameof(IDataRecord.GetGuid),
                "Single" => nameof(IDataRecord.GetFloat),
                "Int16" => nameof(IDataRecord.GetInt16),
                "Int32" => nameof(IDataRecord.GetInt32),
                "Int64" => nameof(IDataRecord.GetInt64),
                "String" => nameof(IDataRecord.GetString),
                _ => throw new Exception("Unsupported")
            }}";

        }
    }

    class PropertyInfo(IPropertySymbol symbol)
    {
        public string Name => symbol.Name;
        public string ColumnName() => symbol.GetAttributes() switch
        {
            [{ AttributeClass.Name: "ColumnAttribute", ConstructorArguments: [{ Value: string n }] }] => n,
            _ => Name
        };

        public string Type => symbol.Type switch
        {
            INamedTypeSymbol { TypeArguments: [ITypeSymbol underlyingType] } => underlyingType.Name,
            _ => symbol.Type.Name
        };
        public bool Nullable => symbol.Type.NullableAnnotation is NullableAnnotation.Annotated;
        public bool IsValueType => symbol.Type.IsValueType;
    }
    internal class MapperInfo
    {
        public string? Namespace { get; }
        
        public string Name { get; }
        
        public bool IsRecord { get; }

        internal NamingConvention NamingConvention { get; }


        public MapperInfo(ITypeSymbol type)
        {
            IsRecord = type.IsRecord;

            Namespace = type.ContainingNamespace.IsGlobalNamespace ? null : type.ContainingNamespace.ToString();

            Name = type.Name;

            if (type.IsRecord && type is INamedTypeSymbol s)
            {
                var properties = type.GetMembers().OfType<IPropertySymbol>().Where(p => p is { SetMethod.IsInitOnly: true,  GetMethod: not null }).ToList();
                var parameters = s.InstanceConstructors.Where(c => c.Parameters.Length == properties.Count).Single().Parameters;
                Properties = (from pa in parameters
                              join pr in properties on pa.Name equals pr.Name
                              select new PropertyInfo(pr)).ToList();
            }
            else
            {
                Properties = type.GetMembers().OfType<IPropertySymbol>()
                    .Select(s => new PropertyInfo(s)).ToList();
            }

            NamingConvention = (
                from a in type.GetAttributes()
                where a.AttributeClass?.Name == "MapFromDataRecordAttribute"
                let columnNamingConvention =
                    (from n in a.NamedArguments
                     where n.Key == "ColumnNamingConvention"
                     select n.Value.Value).FirstOrDefault()
                select (NamingConvention?) ((int?)columnNamingConvention)
                ).SingleOrDefault() ?? NamingConvention.PascalCase;
        }
 
        public IEnumerable<PropertyInfo> Properties { get; }
    }
}

enum NamingConvention
{
    PascalCase = 0,
    lowercase = 1,
    UPPERCASE = 2
}

internal static class StringExtensions
{
    public static string Transform(this string propertyName, NamingConvention namingConvention)
    {
        return namingConvention switch
        {
            NamingConvention.lowercase => propertyName.ToLowerWithUnderscores(),
            NamingConvention.UPPERCASE => propertyName.ToUpperWithUnderscores(),
            _ => propertyName
        };
    }

    public static string ToUpperWithUnderscores(this string source) => string.Join("_", SplitUpperCase(source).Select(s => s.ToUpperInvariant()));
    public static string ToLowerWithUnderscores(this string source) => string.Join("_", SplitUpperCase(source).Select(s => s.ToLowerInvariant()));
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
