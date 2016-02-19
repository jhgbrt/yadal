using System;
using System.Collections.Generic;
using System.Data;

namespace Net.Code.ADONet.Extensions.Experimental
{
    public static class DbExtensions
    {
        [Obsolete("This is an experimental feature, API may change or be removed in future versions", false)]
        public static void Insert<T>(this IDb db, IEnumerable<T> items)
        {
            // TODO we probably don't want this to be static
            var query = Query<T>.Insert(db.ProviderName);
            db.Connect();
            using (var tx = db.Connection.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                var commandBuilder = db.Sql(query).InTransaction(tx);
                foreach (var item in items)
                {
                    commandBuilder.WithParameters(item).AsNonQuery();
                }
                tx.Commit();
            }
        }
    }
}
