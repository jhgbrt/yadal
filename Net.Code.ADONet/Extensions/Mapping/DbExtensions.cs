namespace Net.Code.ADONet;
public static class DbExtensions
{
    /// <summary>
    /// Insert a list of items
    /// </summary>
    public static void Insert<T>(this IDb db, IEnumerable<T> items) => Do(db, items, QueryFactory<T>.INSERT(db.MappingConvention));
    /// <summary>
    /// Insert a list of items
    /// </summary>
    public static ValueTask InsertAsync<T>(this IDb db, IEnumerable<T> items) => DoAsync(db, items, QueryFactory<T>.INSERT(db.MappingConvention));
    /// <summary>
    /// Update a list of items
    /// </summary>
    public static void Update<T>(this IDb db, IEnumerable<T> items) => Do(db, items, QueryFactory<T>.UPDATE(db.MappingConvention));
    /// <summary>
    /// Update a list of items
    /// </summary>
    public static ValueTask UpdateAsync<T>(this IDb db, IEnumerable<T> items) => DoAsync(db, items, QueryFactory<T>.UPDATE(db.MappingConvention));

    /// <summary>
    /// Select all items from a table
    /// </summary>
    public static List<T> SelectAll<T>(this IDb db) => db.Sql(QueryFactory<T>.SELECTALL(db.MappingConvention)).AsEnumerable<T>().ToList();
    /// <summary>
    /// Select all items from a table
    /// </summary>
    public static IAsyncEnumerable<T> SelectAllAsync<T>(this IDb db) => db.Sql(QueryFactory<T>.SELECTALL(db.MappingConvention)).AsEnumerableAsync<T>();

    /// <summary>
    /// Select a single item.
    /// </summary>
    public static T? SelectOne<T>(this IDb db, object key) => db
        .Sql(QueryFactory<T>.SELECTONE(db.MappingConvention))
        .WithKey(QueryFactory<T>.ToKey(key)
        ).AsEnumerable<T>().FirstOrDefault();
    /// <summary>
    /// Select a single item.
    /// </summary>
    public static ValueTask<T?> SelectOneAsync<T>(this IDb db, object key) => db
        .Sql(QueryFactory<T>.SELECTONE(db.MappingConvention))
        .WithKey(QueryFactory<T>.ToKey(key)
        ).AsEnumerableAsync<T>().FirstOrDefaultAsync();

    /// <summary>
    /// Delete a list of items
    /// </summary>
    public static void Delete<T>(this IDb db, IEnumerable<T> items) => Do(db, items, QueryFactory<T>.DELETE(db.MappingConvention));
    /// <summary>
    /// Delete a list of items
    /// </summary>
    public static async Task DeleteAsync<T>(this IDb db, IEnumerable<T> items) => await DoAsync(db, items, QueryFactory<T>.DELETE(db.MappingConvention)).ConfigureAwait(false);

    /// <summary>
    /// Count
    /// </summary>
    public static int Count<T>(this IDb db) => db.Sql(QueryFactory<T>.COUNT(db.MappingConvention)).AsScalar<int>();

    /// <summary>
    /// Count
    /// </summary>
    public static Task<int> CountAsync<T>(this IDb db) => db.Sql(QueryFactory<T>.COUNT(db.MappingConvention)).AsScalarAsync<int>();


    private static void Do<T>(IDb db, IEnumerable<T> items, string query)
    {
        using var commandBuilder = db.Sql(query);
        foreach (var item in items)
        {
            commandBuilder.WithParameters(item).AsNonQuery();
        }
    }
    private static async ValueTask DoAsync<T>(IDb db, IEnumerable<T> items, string query)
    {
        using var commandBuilder = db.Sql(query);
        foreach (var item in items)
        {
            await commandBuilder.WithParameters(item).AsNonQueryAsync().ConfigureAwait(false);
        }
    }
}
