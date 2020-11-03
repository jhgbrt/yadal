using System.Collections.Generic;
using System.Threading.Tasks;

namespace Net.Code.ADONet.Extensions.Mapping
{
    public static class DbExtensions
    {
        /// <summary>
        /// Please note: this is an experimental feature, API may change or be removed in future versions"
        /// </summary>
        public static void Insert<T>(this IDb db, IEnumerable<T> items)
        {
            var query = Query<T>.Create(((Db)db).MappingConvention).Insert;
            Do(db, items, query);
        }

        /// <summary>
        /// Please note: this is an experimental feature, API may change or be removed in future versions"
        /// </summary>
        public static async Task InsertAsync<T>(this IDb db, IEnumerable<T> items)
        {
            var query = Query<T>.Create(((Db)db).MappingConvention).Insert;
            await DoAsync(db, items, query).ConfigureAwait(false);
        }

        /// <summary>
        /// Please note: this is an experimental feature, API may change or be removed in future versions"
        /// </summary>
        public static void Update<T>(this IDb db, IEnumerable<T> items)
        {
            var query = Query<T>.Create(((Db)db).MappingConvention).Update;
            Do(db, items, query);
        }
        /// <summary>
        /// Please note: this is an experimental feature, API may change or be removed in future versions"
        /// </summary>
        public static async Task UpdateAsync<T>(this IDb db, IEnumerable<T> items)
        {
            var query = Query<T>.Create(((Db)db).MappingConvention).Update;
            await DoAsync(db, items, query).ConfigureAwait(false);
        }

        /// <summary>
        /// Please note: this is an experimental feature, API may change or be removed in future versions"
        /// </summary>
        public static void Delete<T>(this IDb db, IEnumerable<T> items)
        {
            var query = Query<T>.Create(((Db)db).MappingConvention).Delete;
            Do(db, items, query);
        }
        /// <summary>
        /// Please note: this is an experimental feature, API may change or be removed in future versions"
        /// </summary>
        public static async Task DeleteAsync<T>(this IDb db, IEnumerable<T> items)
        {
            var query = Query<T>.Create(((Db)db).MappingConvention).Delete;
            await DoAsync(db, items, query).ConfigureAwait(false);
        }

        private static void Do<T>(IDb db, IEnumerable<T> items, string query)
        {
            var commandBuilder = db.Sql(query);
            foreach (var item in items)
            {
                commandBuilder.WithParameters(item).AsNonQuery();
            }
        }
        private static async Task DoAsync<T>(IDb db, IEnumerable<T> items, string query)
        {
            var commandBuilder = db.Sql(query);
            foreach (var item in items)
            {
                await commandBuilder.WithParameters(item).AsNonQueryAsync().ConfigureAwait(false);
            }
        }
    }
}
