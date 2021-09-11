using System.Dynamic;

namespace Net.Code.ADONet;

internal static class Dynamic
{
    public static dynamic From(DataRow row) => From(row, (r, s) => r[s], row.Table.Columns.OfType<DataColumn>().Select(c => c.ColumnName));
    public static dynamic From(IDataRecord record) => From(record, (r, s) => r[s], GetMemberNames(record));
    public static dynamic From<TValue>(IDictionary<string, TValue> dictionary) => From(dictionary, (d, s) => d[s], dictionary.Keys);
    private static IEnumerable<string> GetMemberNames(IDataRecord record)
    {
        for (int i = 0; i < record.FieldCount; i++) yield return record.GetName(i);
    }
    private static dynamic From<T>(T item, Func<T, string, object?> getter, IEnumerable<string> memberNames) 
        => new DynamicIndexer<T>(item, getter, memberNames);

    private class DynamicIndexer<T> : DynamicObject
    {
        private readonly T _item;
        private readonly Func<T, string, object?> _getter;
        private readonly IEnumerable<string> _memberNames;

        public DynamicIndexer(T item, Func<T, string, object?> getter, IEnumerable<string> memberNames)
        {
            _item = item;
            _getter = getter;
            _memberNames = memberNames;
        }

        public sealed override bool TryGetIndex(GetIndexBinder b, object[] i, out object? r) => ByMemberName(out r, (string)i[0]);
        public sealed override bool TryGetMember(GetMemberBinder b, out object? r) => ByMemberName(out r, b.Name);
        public sealed override IEnumerable<string> GetDynamicMemberNames() => _memberNames;

        private bool ByMemberName(out object? result, string memberName)
        {
            var value = _getter(_item, memberName);
            result = DBNullHelper.FromDb(value);
            return true;
        }
    }
}
