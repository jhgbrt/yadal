using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Net.Code.ADONet;

public record Query(string Insert, string Update, string Delete, string Select, string SelectAll, string Count);

internal sealed class QueryFactory<T> 
{

    internal static Query Create(MappingConvention convention)
    {
        var properties = typeof(T).GetProperties();
        var keyProperties = properties.Where(p => p.CustomAttributes.Any(a => a.AttributeType == typeof(KeyAttribute)));
        if (!keyProperties.Any())
            keyProperties = properties.Where(p => p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase));
        if (!keyProperties.Any())
            keyProperties = properties.Where(p => p.Name.Equals($"{typeof(T).Name}Id", StringComparison.OrdinalIgnoreCase)).ToArray();

        var dbGenerated = keyProperties.Where(p => p.HasCustomAttribute<DatabaseGeneratedAttribute>(a => a.DatabaseGeneratedOption != DatabaseGeneratedOption.None));
        var nonKeyProperties = properties.Except(keyProperties);
        var insertProperties = properties.Except(dbGenerated);

        var insertColumns = string.Join(", ", insertProperties.Select(p => p.GetColumnName(convention)));
        var insertValues = string.Join(", ", insertProperties.Select(p => $"{convention.Parameter(p.Name)}"));
        var whereClause = string.Join(" AND ", keyProperties.Select(p => $"{p.GetColumnName(convention)} = {convention.Parameter(p.Name)}"));
        var updateColumns = string.Join(", ", nonKeyProperties.Select(p => $"{p.GetColumnName(convention)} = {convention.Parameter(p.Name)}"));
        var allColumns = string.Join(", ", properties.Select(p => p.GetColumnName(convention)));
        var tableName = typeof(T).GetTableName(convention);

        return new Query(
            $"INSERT INTO {tableName} ({insertColumns}) VALUES ({insertValues})", 
            $"UPDATE {tableName} SET {updateColumns} WHERE {whereClause}", 
            $"DELETE FROM {tableName} WHERE {whereClause}", 
            $"SELECT {allColumns} FROM {tableName} WHERE {whereClause}", 
            $"SELECT {allColumns} FROM {tableName}", 
            $"SELECT COUNT(*) FROM {tableName}");
    }

}
