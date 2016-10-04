using System.Collections.Generic;

namespace Net.Code.ADONet.Extensions.Experimental
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
        public static void Update<T>(this IDb db, IEnumerable<T> items)
        {
            var query = Query<T>.Create(((Db)db).MappingConvention).Update;
            Do(db, items, query);
        }

        /// <summary>
        /// Please note: this is an experimental feature, API may change or be removed in future versions"
        /// </summary>
        public static void Delete<T>(this IDb db, IEnumerable<T> items)
        {
            var query = Query<T>.Create(((Db)db).MappingConvention).Delete;
            Do(db, items, query);
        }

        private static void Do<T>(IDb db, IEnumerable<T> items, string query)
        {
            var commandBuilder = db.Sql(query);
            foreach (var item in items)
            {
                commandBuilder.WithParameters(item).AsNonQuery();
            }
        }

    }
}
