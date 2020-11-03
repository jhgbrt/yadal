using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;

namespace Net.Code.ADONet
{
    internal static class Dynamic
    {
        public static dynamic From(DataRow row) => From(row, (r, s) => r[s]);
        public static dynamic From(IDataRecord record) => From(record, (r, s) => r[s]);
        public static dynamic From<TValue>(IDictionary<string, TValue> dictionary) => From(dictionary, (d, s) => d[s]);

        private static dynamic From<T>(T item, Func<T, string, object?> getter) => new DynamicIndexer<T>(item, getter);

        private class DynamicIndexer<T> : DynamicObject
        {
            private readonly T _item;
            private readonly Func<T, string, object?> _getter;

            public DynamicIndexer(T item, Func<T, string, object?> getter)
            {
                _item = item;
                _getter = getter;
            }

            public sealed override bool TryGetIndex(GetIndexBinder b, object[] i, out object? r) => ByMemberName(out r, (string)i[0]);
            public sealed override bool TryGetMember(GetMemberBinder b, out object? r) => ByMemberName(out r, b.Name);
            private bool ByMemberName(out object? result, string memberName)
            {
                var value = _getter(_item, memberName);
                result = DBNullHelper.FromDb(value);
                return true;
            }
        }
    }
}