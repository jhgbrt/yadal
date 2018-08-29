using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace Net.Code.ADONet
{
    public static class ExtensionsForDataSetRelatedStuff
    {
        static dynamic ToDynamic(this DataRow dr) => Dynamic.From(dr);
        public static IEnumerable<dynamic> AsEnumerable(this DataTable dataTable) => dataTable.Rows.OfType<DataRow>().Select(ToDynamic);
        public static IEnumerable<T> Select<T>(this DataTable dt, Func<dynamic, T> selector) => dt.AsEnumerable().Select(selector);
        public static IEnumerable<dynamic> Where(this DataTable dt, Func<dynamic, bool> predicate) => dt.AsEnumerable().Where(predicate);

        /// <summary>
        /// Executes the query (using datareader) and fills a datatable
        /// </summary>
        public static DataTable AsDataTable(this CommandBuilder commandBuilder)
        {
            using (var reader = commandBuilder.AsReader())
            {
                var tb = new DataTable();
                tb.Load(reader);
                return tb;
            }
        }
        public static DataTable ToDataTable<T>(this IEnumerable<T> items)
        {
            var table = new DataTable(typeof(T).Name);

            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in props)
            {
                var propType = prop.PropertyType.GetUnderlyingType();
                table.Columns.Add(prop.Name, propType);
            }

            var values = new object[props.Length];
            foreach (var item in items)
            {
                for (var i = 0; i < props.Length; i++)
                    values[i] = props[i].GetValue(item, null);
                table.Rows.Add(values);
            }
            return table;
        }
    }
}