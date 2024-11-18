﻿//HintName: My.Namespace.PersonMapFromDataRecordExtension.g.cs
// <auto-generated>
using System.Data;

namespace My.Namespace;
public static partial class PersonMapper
{
    static class Ordinal
    {
        public static bool __initialized__;
        public static int Id;
        public static int Age;
        public static int Name;
    }
    public static Person ToPerson(this IDataRecord record)
    {
        if (!Ordinal.__initialized__)
        {
            Ordinal.Id = record.GetOrdinal("ID");
            Ordinal.Age = record.GetOrdinal("AGE");
            Ordinal.Name = record.GetOrdinal("NAME");
            System.Threading.Thread.MemoryBarrier();
            Ordinal.__initialized__ = true;
        }
        return new Person (record.GetInt32(Ordinal.Id),record.IsDBNull(Ordinal.Age) ? null : record.GetInt32(Ordinal.Age),record.GetString(Ordinal.Name));
    }
}
