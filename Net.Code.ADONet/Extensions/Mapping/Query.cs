using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Net.Code.ADONet;

public record Query(string Insert, string Update, string Delete, string Select, string SelectAll, string Count);

internal sealed class QueryFactory<T> 
{
    private static readonly string[] AllPropertyNames;
    private static readonly string[] InsertPropertyNames;
    private static readonly string[] KeyPropertyNames;
    private static readonly string[] NonKeyPropertyNames;

    internal static Query Create(MappingConvention convention)
    {
        var insertColumns = string.Join(", ", QueryFactory<T>.InsertPropertyNames.Select(convention.ToDb));
        var insertValues = string.Join(", ", InsertPropertyNames.Select(s => $"{convention.Parameter(s)}"));
        var whereClause = string.Join(" AND ", KeyPropertyNames.Select(s => $"{convention.ToDb(s)} = {convention.Parameter(s)}"));
        var updateColumns = string.Join(", ", NonKeyPropertyNames.Select(s => $"{convention.ToDb(s)} = {convention.Parameter(s)}"));
        var allColumns = string.Join(", ", QueryFactory<T>.AllPropertyNames.Select(convention.ToDb));
        var tableName = convention.ToDb(typeof(T).Name);

        return new Query(
            $"INSERT INTO {tableName} ({insertColumns}) VALUES ({insertValues})", 
            $"UPDATE {tableName} SET {updateColumns} WHERE {whereClause}", 
            $"DELETE FROM {tableName} WHERE {whereClause}", 
            $"SELECT {allColumns} FROM {tableName} WHERE {whereClause}", 
            $"SELECT {allColumns} FROM {tableName}", 
            $"SELECT COUNT(*) FROM {tableName}");
    }

    static QueryFactory()
    {
        var properties = typeof(T).GetProperties();

        var keyProperties = properties.Where(p => p.CustomAttributes.Any(a => a.AttributeType == typeof(KeyAttribute)));
        if (!keyProperties.Any())
            keyProperties = properties.Where(p => p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase));
        if (!keyProperties.Any())
            keyProperties = properties.Where(p => p.Name.Equals($"{typeof(T).Name}Id", StringComparison.OrdinalIgnoreCase)).ToArray();

        var dbGenerated = keyProperties.Where(p => p.HasCustomAttribute<DatabaseGeneratedAttribute>(a => a.DatabaseGeneratedOption != DatabaseGeneratedOption.None));
        var nonKeyProperties = properties.Except(keyProperties);

        AllPropertyNames = properties.Select(p => p.Name).ToArray();
        InsertPropertyNames = properties.Except(dbGenerated).Select(p => p.Name).ToArray();
        KeyPropertyNames = keyProperties.Select(p => p.Name).ToArray();
        NonKeyPropertyNames = nonKeyProperties.Select(p => p.Name).ToArray();
    }
}
