﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Text;

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
                .WriteLine($"public static class __{mapper.Name}Extension__")
                .WriteOpeningBracket();


            builder.WriteLine($"static class ordinal")
                .WriteOpeningBracket()
                .WriteLine("public static bool __initialized__;");

            foreach (var p in mapper.Properties)
            {
                builder.WriteLine($"public static int {p.Name};");
            }

            builder.WriteClosingBracket();

            builder.WriteLine($"public static {mapper.Name} To{mapper.Name}(this IDataRecord record)")
                .WriteOpeningBracket()
                .WriteLine("if (!ordinal.__initialized__)")
                .WriteOpeningBracket();
            foreach (var p in mapper.Properties)
            {
                builder.WriteLine($"ordinal.{p.Name} = record.GetOrdinal(nameof({mapper.Name}.{p.Name}));");
            }
            builder
            .WriteLine("System.Threading.Thread.MemoryBarrier();")
            .WriteLine($"ordinal.__initialized__ = true;")
            .WriteClosingBracket();

            builder.WriteLine($"return new {mapper.Name}")
            .WriteOpeningBracket();

            foreach (var p in mapper.Properties)
            {
                if (p.Nullable && p.IsValueType)
                {
                    builder.WriteLine($"{p.Name} = record.IsDBNull(ordinal.{p.Name}) ? null : record.{GetGetMethod(p)}(ordinal.{p.Name}),");
                }
                else
                {
                    builder.WriteLine($"{p.Name} = record.{GetGetMethod(p)}(ordinal.{p.Name}),");
                }
            }

            builder.WriteClosingBracket(true)
                .WriteClosingBracket()
                .WriteClosingBracket()
            ;

            return new(builder.ToString(), $"{mapper.Namespace??"global"}.{mapper.Name}MapFromDataRecordExtension.g.cs");


        }
        public static string GetGetMethod(PropertyInfo property)
        {
            return $"{property.SpecialType switch
            {
                SpecialType.System_Boolean => nameof(IDataRecord.GetBoolean),
                SpecialType.System_Byte => nameof(IDataRecord.GetByte),
                SpecialType.System_Char => nameof(IDataRecord.GetChar),
                SpecialType.System_DateTime => nameof(IDataRecord.GetDateTime),
                SpecialType.System_Decimal => nameof(IDataRecord.GetDecimal),
                SpecialType.System_Double => nameof(IDataRecord.GetDouble),
                SpecialType.System_Single => nameof(IDataRecord.GetFloat),
                SpecialType.System_Int16 => nameof(IDataRecord.GetInt16),
                SpecialType.System_Int32 => nameof(IDataRecord.GetInt32),
                SpecialType.System_Int64 => nameof(IDataRecord.GetInt64),
                SpecialType.System_String => nameof(IDataRecord.GetString),
                _ => throw new Exception("Unsupported")
            }}";

        }
    }

    public class PropertyInfo
    {
        public PropertyInfo(IPropertySymbol symbol)
        {
            Name = symbol.Name;
            Type = symbol.Type.Name;
            Nullable = symbol.Type.NullableAnnotation is NullableAnnotation.Annotated;
            IsValueType = symbol.Type.IsValueType;
            SpecialType = symbol.Type switch
            {
                INamedTypeSymbol t => t.TypeArguments switch
                {
                    [var s] => s.SpecialType,
                    _ => t.SpecialType
                },
                var t => t.SpecialType
            };
        }
        public string Name { get; }
        public string Type { get; }
        public bool Nullable { get; }
        public bool IsValueType { get; }
        public SpecialType SpecialType { get; }
    }
    public class MapperInfo
    {
        public string? Namespace { get; }
        public string Name { get; }
        public MapperInfo(ITypeSymbol type)
        {
            Namespace = type.ContainingNamespace.IsGlobalNamespace ? null : type.ContainingNamespace.ToString();
            Name = type.Name;
            Properties = type.GetMembers().OfType<IPropertySymbol>().Select(s => new PropertyInfo(s)).ToList();
        }
 
        public IEnumerable<PropertyInfo> Properties { get; }
    }
}

