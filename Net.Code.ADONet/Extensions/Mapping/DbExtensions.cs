namespace Net.Code.ADONet;
public static class DbExtensions
{
    /// <summary>
    /// Insert a list of items
    /// </summary>
    public static void Insert<T>(this IDb db, IEnumerable<T> items) => Do(db, items, QueryFactory<T>.Create(db.MappingConvention).Insert);
    /// <summary>
    /// Insert a list of items
    /// </summary>
    public static async Task InsertAsync<T>(this IDb db, IEnumerable<T> items) => await DoAsync(db, items, QueryFactory<T>.Create(db.MappingConvention).Insert).ConfigureAwait(false);
    /// <summary>
    /// Update a list of items
    /// </summary>
    public static void Update<T>(this IDb db, IEnumerable<T> items) => Do(db, items, QueryFactory<T>.Create(db.MappingConvention).Update);
    /// <summary>
    /// Update a list of items
    /// </summary>
    public static async Task UpdateAsync<T>(this IDb db, IEnumerable<T> items) => await DoAsync(db, items, QueryFactory<T>.Create(db.MappingConvention).Update).ConfigureAwait(false);
    /// <summary>
    /// Delete a list of items
    /// </summary>
    public static void Delete<T>(this IDb db, IEnumerable<T> items) => Do(db, items, QueryFactory<T>.Create(db.MappingConvention).Delete);
    /// <summary>
    /// Delete a list of items
    /// </summary>
    public static async Task DeleteAsync<T>(this IDb db, IEnumerable<T> items) => await DoAsync(db, items, QueryFactory<T>.Create(db.MappingConvention).Delete).ConfigureAwait(false);

    private static void Do<T>(IDb db, IEnumerable<T> items, string query)
    {
        using var commandBuilder = db.Sql(query);
        foreach (var item in items)
        {
            commandBuilder.WithParameters(item).AsNonQuery();
        }
    }
    private static async Task DoAsync<T>(IDb db, IEnumerable<T> items, string query)
    {
        using var commandBuilder = db.Sql(query);
        foreach (var item in items)
        {
            await commandBuilder.WithParameters(item).AsNonQueryAsync().ConfigureAwait(false);
        }
    }
}
