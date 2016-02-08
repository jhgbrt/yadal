using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Linq;

namespace Net.Code.ADONet.Tests.Unit
{
    class FakeParameterCollection : DbParameterCollection
    {
        readonly Collection<object> _innerCollection = new Collection<object>();

        public override int Add(object value)
        {
            _innerCollection.Add(value);
            return _innerCollection.Count - 1;
        }

        public override bool Contains(object value) => _innerCollection.Contains(value);

        public override void Clear() => _innerCollection.Clear();

        public override int IndexOf(object value) => _innerCollection.IndexOf(value);

        public override void Insert(int index, object value) => _innerCollection.Insert(index, value);

        public override void Remove(object value) => _innerCollection.Remove(value);

        public override void RemoveAt(int index) => _innerCollection.RemoveAt(index);

        public override void RemoveAt(string parameterName) => _innerCollection.RemoveAt(IndexOf(parameterName));

        protected override void SetParameter(int index, DbParameter value) => _innerCollection[index] = value;

        protected override void SetParameter(string parameterName, DbParameter value) => _innerCollection[IndexOf(parameterName)] = value;

        public override int Count => _innerCollection.Count;

        public override object SyncRoot => _innerCollection;

        public override int IndexOf(string parameterName) => _innerCollection.Select((x, i) => new {((DbParameter) x).ParameterName, i}).FirstOrDefault(x => x.ParameterName == parameterName)?.i ?? -1;

        public override IEnumerator GetEnumerator() => _innerCollection.GetEnumerator();

        protected override DbParameter GetParameter(int index) => (DbParameter) _innerCollection[index];

        protected override DbParameter GetParameter(string parameterName) => (DbParameter) _innerCollection[IndexOf(parameterName)];

        public override bool Contains(string value) => IndexOf(value) != -1;

        public override void CopyTo(Array array, int index) => ((ICollection)_innerCollection).CopyTo(array, index);

        public override void AddRange(Array values)
        {
            foreach (var v in values)
                Add(v);
        }
    }
}
