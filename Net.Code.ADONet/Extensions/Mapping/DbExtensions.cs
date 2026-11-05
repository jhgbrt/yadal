using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Net.Code.ADONet;

/// <summary>
/// Simple CRUD helpers. Table and column names follow the mapping convention of the database
/// ([Table] and [Column] attributes override them); the key is the property marked with [Key],
/// or else the property named 'Id' or '{TypeName}Id'.
/// </summary>
public static class DbExtensions
{
    extension(IDb db)
    {
        /// <summary>
        /// Insert a list of items. When the provider supports it, the statements are sent in batches.
        /// </summary>
        [RequiresUnreferencedCode(Trimming.ReflectionMessage)]
        [RequiresDynamicCode(Trimming.DynamicCodeMessage)]
        public void Insert<T>(IEnumerable<T> items) => Do(db, QueryFactory<T>.INSERT(db.MappingConvention), QueryFactory<T>.InsertParameters(items));
        /// <summary>
        /// Insert a list of items. When the provider supports it, the statements are sent in batches.
        /// </summary>
        [RequiresUnreferencedCode(Trimming.ReflectionMessage)]
        [RequiresDynamicCode(Trimming.DynamicCodeMessage)]
        public ValueTask InsertAsync<T>(IEnumerable<T> items) => db.InsertAsync(items, CancellationToken.None);
        /// <summary>
        /// Insert a list of items. When the provider supports it, the statements are sent in batches.
        /// </summary>
        [RequiresUnreferencedCode(Trimming.ReflectionMessage)]
        [RequiresDynamicCode(Trimming.DynamicCodeMessage)]
        public ValueTask InsertAsync<T>(IEnumerable<T> items, CancellationToken cancellationToken) => DoAsync(db, QueryFactory<T>.INSERT(db.MappingConvention), QueryFactory<T>.InsertParameters(items), cancellationToken);
        /// <summary>
        /// Update a list of items. When the provider supports it, the statements are sent in batches.
        /// </summary>
        [RequiresUnreferencedCode(Trimming.ReflectionMessage)]
        [RequiresDynamicCode(Trimming.DynamicCodeMessage)]
        public void Update<T>(IEnumerable<T> items) => Do(db, QueryFactory<T>.UPDATE(db.MappingConvention), QueryFactory<T>.UpdateParameters(items));
        /// <summary>
        /// Update a list of items. When the provider supports it, the statements are sent in batches.
        /// </summary>
        [RequiresUnreferencedCode(Trimming.ReflectionMessage)]
        [RequiresDynamicCode(Trimming.DynamicCodeMessage)]
        public ValueTask UpdateAsync<T>(IEnumerable<T> items) => db.UpdateAsync(items, CancellationToken.None);
        /// <summary>
        /// Update a list of items. When the provider supports it, the statements are sent in batches.
        /// </summary>
        [RequiresUnreferencedCode(Trimming.ReflectionMessage)]
        [RequiresDynamicCode(Trimming.DynamicCodeMessage)]
        public ValueTask UpdateAsync<T>(IEnumerable<T> items, CancellationToken cancellationToken) => DoAsync(db, QueryFactory<T>.UPDATE(db.MappingConvention), QueryFactory<T>.UpdateParameters(items), cancellationToken);

        /// <summary>
        /// Select all items from a table
        /// </summary>
        [RequiresUnreferencedCode(Trimming.ReflectionMessage)]
        [RequiresDynamicCode(Trimming.DynamicCodeMessage)]
        public List<T> SelectAll<T>() => db.Sql(QueryFactory<T>.SELECTALL(db.MappingConvention)).AsEnumerable<T>().ToList();
        /// <summary>
        /// Select all items from a table
        /// </summary>
        [RequiresUnreferencedCode(Trimming.ReflectionMessage)]
        [RequiresDynamicCode(Trimming.DynamicCodeMessage)]
        public IAsyncEnumerable<T> SelectAllAsync<T>() => db.SelectAllAsync<T>(CancellationToken.None);
        /// <summary>
        /// Select all items from a table
        /// </summary>
        [RequiresUnreferencedCode(Trimming.ReflectionMessage)]
        [RequiresDynamicCode(Trimming.DynamicCodeMessage)]
        public IAsyncEnumerable<T> SelectAllAsync<T>(CancellationToken cancellationToken) => db.Sql(QueryFactory<T>.SELECTALL(db.MappingConvention)).AsEnumerableAsync<T>(cancellationToken);

        /// <summary>
        /// Select a single item.
        /// </summary>
        [RequiresUnreferencedCode(Trimming.ReflectionMessage)]
        [RequiresDynamicCode(Trimming.DynamicCodeMessage)]
        public T? SelectOne<T>(object key) => db
            .Sql(QueryFactory<T>.SELECTONE(db.MappingConvention))
            .WithKey(QueryFactory<T>.ToKey(key))
            .AsEnumerable<T>().FirstOrDefault();
        /// <summary>
        /// Select a single item.
        /// </summary>
        [RequiresUnreferencedCode(Trimming.ReflectionMessage)]
        [RequiresDynamicCode(Trimming.DynamicCodeMessage)]
        public ValueTask<T?> SelectOneAsync<T>(object key) => db.SelectOneAsync<T>(key, CancellationToken.None);
        /// <summary>
        /// Select a single item.
        /// </summary>
        [RequiresUnreferencedCode(Trimming.ReflectionMessage)]
        [RequiresDynamicCode(Trimming.DynamicCodeMessage)]
        public ValueTask<T?> SelectOneAsync<T>(object key, CancellationToken cancellationToken) => AsyncEnumerableHelpers.FirstOrDefaultAsync(
            db.Sql(QueryFactory<T>.SELECTONE(db.MappingConvention))
              .WithKey(QueryFactory<T>.ToKey(key))
              .AsEnumerableAsync<T>(cancellationToken),
            cancellationToken);

        /// <summary>
        /// Delete a list of items. When the provider supports it, the statements are sent in batches.
        /// </summary>
        [RequiresUnreferencedCode(Trimming.ReflectionMessage)]
        [RequiresDynamicCode(Trimming.DynamicCodeMessage)]
        public void Delete<T>(IEnumerable<T> items) => Do(db, QueryFactory<T>.DELETE(db.MappingConvention), QueryFactory<T>.DeleteParameters(items));
        /// <summary>
        /// Delete a list of items. When the provider supports it, the statements are sent in batches.
        /// </summary>
        [RequiresUnreferencedCode(Trimming.ReflectionMessage)]
        [RequiresDynamicCode(Trimming.DynamicCodeMessage)]
        public Task DeleteAsync<T>(IEnumerable<T> items) => db.DeleteAsync(items, CancellationToken.None);
        /// <summary>
        /// Delete a list of items. When the provider supports it, the statements are sent in batches.
        /// </summary>
        [RequiresUnreferencedCode(Trimming.ReflectionMessage)]
        [RequiresDynamicCode(Trimming.DynamicCodeMessage)]
        public async Task DeleteAsync<T>(IEnumerable<T> items, CancellationToken cancellationToken) => await DoAsync(db, QueryFactory<T>.DELETE(db.MappingConvention), QueryFactory<T>.DeleteParameters(items), cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Count
        /// </summary>
        [RequiresUnreferencedCode(Trimming.ReflectionMessage)]
        [RequiresDynamicCode(Trimming.DynamicCodeMessage)]
        public int Count<T>() => db.Sql(QueryFactory<T>.COUNT(db.MappingConvention)).AsScalar<int>();

        /// <summary>
        /// Count
        /// </summary>
        [RequiresUnreferencedCode(Trimming.ReflectionMessage)]
        [RequiresDynamicCode(Trimming.DynamicCodeMessage)]
        public Task<int> CountAsync<T>() => db.CountAsync<T>(CancellationToken.None);

        /// <summary>
        /// Count
        /// </summary>
        [RequiresUnreferencedCode(Trimming.ReflectionMessage)]
        [RequiresDynamicCode(Trimming.DynamicCodeMessage)]
        public Task<int> CountAsync<T>(CancellationToken cancellationToken) => db.Sql(QueryFactory<T>.COUNT(db.MappingConvention)).AsScalarAsync<int>(cancellationToken);
    }

    private static void Do(IDb db, string query, IEnumerable<IEnumerable<(string name, object? value)>> parameterSets)
    {
        if (db is Db concrete)
        {
            concrete.ExecuteForEach(query, parameterSets);
            return;
        }
        using var commandBuilder = db.Sql(query);
        foreach (var parameters in parameterSets)
        {
            foreach (var (name, value) in parameters)
                commandBuilder.WithParameter(name, value);
            commandBuilder.AsNonQuery();
        }
    }

    private static async ValueTask DoAsync(IDb db, string query, IEnumerable<IEnumerable<(string name, object? value)>> parameterSets, CancellationToken cancellationToken)
    {
        if (db is Db concrete)
        {
            await concrete.ExecuteForEachAsync(query, parameterSets, cancellationToken).ConfigureAwait(false);
            return;
        }
        using var commandBuilder = db.Sql(query);
        foreach (var parameters in parameterSets)
        {
            foreach (var (name, value) in parameters)
                commandBuilder.WithParameter(name, value);
            await commandBuilder.AsNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
