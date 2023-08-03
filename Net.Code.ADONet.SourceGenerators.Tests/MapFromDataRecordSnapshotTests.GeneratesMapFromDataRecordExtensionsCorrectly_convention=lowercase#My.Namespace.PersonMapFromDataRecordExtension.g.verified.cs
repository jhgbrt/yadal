﻿//HintName: My.Namespace.PersonMapFromDataRecordExtension.g.cs
// <auto-generated>
using System.Data;

namespace My.Namespace;
public static class __PersonExtension__
{
    static class ordinal
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
        if (!ordinal.__initialized__)
        {
            ordinal.Id = record.GetOrdinal("id");
            ordinal.Age = record.GetOrdinal("age");
            ordinal.IsGood = record.GetOrdinal("is_good");
            ordinal.IsBad = record.GetOrdinal("is_bad");
            ordinal.BirthDate = record.GetOrdinal("birth_date");
            ordinal.LastSeen = record.GetOrdinal("last_seen");
            ordinal.Name = record.GetOrdinal("name");
            ordinal.MiddleName = record.GetOrdinal("middle_name");
            System.Threading.Thread.MemoryBarrier();
            ordinal.__initialized__ = true;
        }
        return new Person
        {
            Id = record.GetInt32(ordinal.Id),
            Age = record.IsDBNull(ordinal.Age) ? null : record.GetInt32(ordinal.Age),
            IsGood = record.GetBoolean(ordinal.IsGood),
            IsBad = record.IsDBNull(ordinal.IsBad) ? null : record.GetBoolean(ordinal.IsBad),
            BirthDate = record.GetDateTime(ordinal.BirthDate),
            LastSeen = record.IsDBNull(ordinal.LastSeen) ? null : record.GetDateTime(ordinal.LastSeen),
            Name = record.GetString(ordinal.Name),
            MiddleName = record.GetString(ordinal.MiddleName),
        };
    }
}