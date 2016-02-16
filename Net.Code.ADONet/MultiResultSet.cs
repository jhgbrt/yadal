using System.Collections.Generic;

namespace Net.Code.ADONet
{
    public static class MultiResultSet
    {
        public static MultiResultSet<T1, T2> Create<T1, T2>(IReadOnlyCollection<T1> set1, IReadOnlyCollection<T2> set2)
        {
            return new MultiResultSet<T1, T2>(set1, set2);
        }
        public static MultiResultSet<T1, T2, T3> Create<T1, T2, T3>(IReadOnlyCollection<T1> set1, IReadOnlyCollection<T2> set2, IReadOnlyCollection<T3> set3)
        {
            return new MultiResultSet<T1, T2, T3>(set1, set2, set3);
        }
        public static MultiResultSet<T1, T2, T3, T4> Create<T1, T2, T3, T4>(IReadOnlyCollection<T1> set1, IReadOnlyCollection<T2> set2, IReadOnlyCollection<T3> set3, IReadOnlyCollection<T4> set4)
        {
            return new MultiResultSet<T1, T2, T3, T4>(set1, set2, set3, set4);
        }
        public static MultiResultSet<T1, T2, T3, T4, T5> Create<T1, T2, T3, T4, T5>(IReadOnlyCollection<T1> set1, IReadOnlyCollection<T2> set2, IReadOnlyCollection<T3> set3, IReadOnlyCollection<T4> set4, IReadOnlyCollection<T5> set5)
        {
            return new MultiResultSet<T1, T2, T3, T4, T5>(set1, set2, set3, set4, set5);
        }
    }

    public sealed class MultiResultSet<T1, T2>
    {
        public MultiResultSet(IReadOnlyCollection<T1> set1, IReadOnlyCollection<T2> set2)
        {
            Set1 = set1;
            Set2 = set2;
        }

        public IReadOnlyCollection<T1> Set1 { get; }
        public IReadOnlyCollection<T2> Set2 { get; }
    }
    public sealed class MultiResultSet<T1, T2, T3>
    {
        public MultiResultSet(IReadOnlyCollection<T1> set1, IReadOnlyCollection<T2> set2, IReadOnlyCollection<T3> set3)
        {
            Set1 = set1;
            Set2 = set2;
            Set3 = set3;
        }

        public IReadOnlyCollection<T1> Set1 { get; }
        public IReadOnlyCollection<T2> Set2 { get; }
        public IReadOnlyCollection<T3> Set3 { get; }
    }
    public sealed class MultiResultSet<T1, T2, T3, T4>
    {
        public MultiResultSet(IReadOnlyCollection<T1> set1, IReadOnlyCollection<T2> set2, IReadOnlyCollection<T3> set3, IReadOnlyCollection<T4> set4)
        {
            Set1 = set1;
            Set2 = set2;
            Set3 = set3;
            Set4 = set4;
        }

        public IReadOnlyCollection<T1> Set1 { get; }
        public IReadOnlyCollection<T2> Set2 { get; }
        public IReadOnlyCollection<T3> Set3 { get; }
        public IReadOnlyCollection<T4> Set4 { get; }
    }

    public sealed class MultiResultSet<T1, T2, T3, T4, T5>
    {
        public MultiResultSet(IReadOnlyCollection<T1> set1, IReadOnlyCollection<T2> set2, IReadOnlyCollection<T3> set3, IReadOnlyCollection<T4> set4, IReadOnlyCollection<T5> set5)
        {
            Set1 = set1;
            Set2 = set2;
            Set3 = set3;
            Set4 = set4;
            Set5 = set5;
        }

        public IReadOnlyCollection<T1> Set1 { get; }
        public IReadOnlyCollection<T2> Set2 { get; }
        public IReadOnlyCollection<T3> Set3 { get; }
        public IReadOnlyCollection<T4> Set4 { get; }
        public IReadOnlyCollection<T5> Set5 { get; }
    }
}