﻿//HintName: My.Namespace.PersonMapFromDataRecordExtension.g.cs
// <auto-generated>
using System.Data;

namespace My.Namespace;
public static partial class Mapper
{
    static class Ordinal
    {
        public static bool __initialized__;
        public static int Id;
        public static int Age;
        public static int IsGood;
        public static int IsBad;
        public static int BirthDate;
        public static int LastSeen;
        public static int Name;
        public static int MiddleName;
    }
    public static Person ToPerson(this IDataRecord record)
    {
        if (!Ordinal.__initialized__)
        {
            Ordinal.Id = record.GetOrdinal("ID");
            Ordinal.Age = record.GetOrdinal("THE_AGE");
            Ordinal.IsGood = record.GetOrdinal("IS_GOOD");
            Ordinal.IsBad = record.GetOrdinal("IS_BAD");
            Ordinal.BirthDate = record.GetOrdinal("BIRTH_DATE");
            Ordinal.LastSeen = record.GetOrdinal("LAST_SEEN");
            Ordinal.Name = record.GetOrdinal("NAME");
            Ordinal.MiddleName = record.GetOrdinal("MIDDLE_NAME");
            System.Threading.Thread.MemoryBarrier();
            Ordinal.__initialized__ = true;
        }
        return new Person
        {
            Id = record.GetInt32(Ordinal.Id),
            Age = record.IsDBNull(Ordinal.Age) ? null : record.GetInt32(Ordinal.Age),
            IsGood = record.GetBoolean(Ordinal.IsGood),
            IsBad = record.IsDBNull(Ordinal.IsBad) ? null : record.GetBoolean(Ordinal.IsBad),
            BirthDate = record.GetDateTime(Ordinal.BirthDate),
            LastSeen = record.IsDBNull(Ordinal.LastSeen) ? null : record.GetDateTime(Ordinal.LastSeen),
            Name = record.GetString(Ordinal.Name),
            MiddleName = record.GetString(Ordinal.MiddleName),
        };
    }
}
