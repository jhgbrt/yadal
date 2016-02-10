using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Net.Code.ADONet
{
    public static class DataTableExtensions
    {
        static dynamic ToDynamic(this DataRow dr) => Dynamic.From(dr);
        public static IEnumerable<dynamic> AsEnumerable(this DataTable dataTable) => dataTable.Rows.OfType<DataRow>().Select(ToDynamic);
        public static IEnumerable<T> Select<T>(this DataTable dt, Func<dynamic, T> selector) => dt.AsEnumerable().Select(selector);
        public static IEnumerable<dynamic> Where(this DataTable dt, Func<dynamic, bool> predicate) => dt.AsEnumerable().Where(predicate);
    }
}