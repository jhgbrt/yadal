namespace Net.Code.ADONet;

internal static class DataTableExtensions
{
    extension(DataTable dataTable)
    {
        public IEnumerable<dynamic> AsEnumerable() => dataTable.Rows.OfType<DataRow>().Select(Dynamic.From);
        public IEnumerable<T> Select<T>(Func<dynamic, T> selector) => dataTable.AsEnumerable().Select(selector);
        public IEnumerable<dynamic> Where(Func<dynamic, bool> predicate) => dataTable.AsEnumerable().Where(predicate);
    }

    extension(CommandBuilder commandBuilder)
    {
        /// <summary>
        /// Executes the query (using datareader) and fills a datatable
        /// </summary>
        public DataTable AsDataTable()
        {
            using var reader = commandBuilder.AsReader();
            var tb = new DataTable();
            tb.Load(reader);
            return tb;
        }
    }

    extension<T>(IEnumerable<T> items)
    {
        public DataTable ToDataTable()
        {
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var table = new DataTable(typeof(T).Name);
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
