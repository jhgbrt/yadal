namespace Net.Code.ADONet;
public static class DbExtensions
{
    extension(IDb db)
    {
        /// <summary>
        /// Insert a list of items
        /// </summary>
        public void Insert<T>(IEnumerable<T> items) => Do(db, items, QueryFactory<T>.INSERT(db.MappingConvention));
        /// <summary>
        /// Insert a list of items
        /// </summary>
        public ValueTask InsertAsync<T>(IEnumerable<T> items) => DoAsync(db, items, QueryFactory<T>.INSERT(db.MappingConvention));
        /// <summary>
        /// Update a list of items
        /// </summary>
        public void Update<T>(IEnumerable<T> items) => Do(db, items, QueryFactory<T>.UPDATE(db.MappingConvention));
        /// <summary>
        /// Update a list of items
        /// </summary>
        public ValueTask UpdateAsync<T>(IEnumerable<T> items) => DoAsync(db, items, QueryFactory<T>.UPDATE(db.MappingConvention));

        /// <summary>
        /// Select all items from a table
        /// </summary>
        public List<T> SelectAll<T>() => db.Sql(QueryFactory<T>.SELECTALL(db.MappingConvention)).AsEnumerable<T>().ToList();
        /// <summary>
        /// Select all items from a table
        /// </summary>
        public IAsyncEnumerable<T> SelectAllAsync<T>() => db.Sql(QueryFactory<T>.SELECTALL(db.MappingConvention)).AsEnumerableAsync<T>();

        /// <summary>
        /// Select a single item.
        /// </summary>
        public T? SelectOne<T>(object key) => db
            .Sql(QueryFactory<T>.SELECTONE(db.MappingConvention))
            .WithKey(QueryFactory<T>.ToKey(key)
            ).AsEnumerable<T>().FirstOrDefault();
        /// <summary>
        /// Select a single item.
        /// </summary>
        public ValueTask<T?> SelectOneAsync<T>(object key) => db
            .Sql(QueryFactory<T>.SELECTONE(db.MappingConvention))
            .WithKey(QueryFactory<T>.ToKey(key)
            ).AsEnumerableAsync<T>().FirstOrDefaultAsync();

        /// <summary>
        /// Delete a list of items
        /// </summary>
        public void Delete<T>(IEnumerable<T> items) => Do(db, items, QueryFactory<T>.DELETE(db.MappingConvention));
        /// <summary>
        /// Delete a list of items
        /// </summary>
        public async Task DeleteAsync<T>(IEnumerable<T> items) => await DoAsync(db, items, QueryFactory<T>.DELETE(db.MappingConvention)).ConfigureAwait(false);

        /// <summary>
        /// Count
        /// </summary>
        public int Count<T>() => db.Sql(QueryFactory<T>.COUNT(db.MappingConvention)).AsScalar<int>();

        /// <summary>
        /// Count
        /// </summary>
        public Task<int> CountAsync<T>() => db.Sql(QueryFactory<T>.COUNT(db.MappingConvention)).AsScalarAsync<int>();
    }

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
