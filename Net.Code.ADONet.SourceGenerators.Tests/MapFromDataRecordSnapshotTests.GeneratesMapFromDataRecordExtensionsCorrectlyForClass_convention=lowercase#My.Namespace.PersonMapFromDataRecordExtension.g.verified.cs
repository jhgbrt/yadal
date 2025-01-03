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
        public static int Float;
        public static int NullableFloat;
        public static int Double;
        public static int NullableDouble;
        public static int Short;
        public static int NullableShort;
        public static int Long;
        public static int NullableLong;
        public static int Char;
        public static int NullableChar;
        public static int Byte;
        public static int NullableByte;
        public static int MyGuid;
        public static int NullableGuid;
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
            Ordinal.Id = record.GetOrdinal("id");
            Ordinal.Age = record.GetOrdinal("the_age");
            Ordinal.Float = record.GetOrdinal("float");
            Ordinal.NullableFloat = record.GetOrdinal("nullable_float");
            Ordinal.Double = record.GetOrdinal("double");
            Ordinal.NullableDouble = record.GetOrdinal("nullable_double");
            Ordinal.Short = record.GetOrdinal("short");
            Ordinal.NullableShort = record.GetOrdinal("nullable_short");
            Ordinal.Long = record.GetOrdinal("long");
            Ordinal.NullableLong = record.GetOrdinal("nullable_long");
            Ordinal.Char = record.GetOrdinal("char");
            Ordinal.NullableChar = record.GetOrdinal("nullable_char");
            Ordinal.Byte = record.GetOrdinal("byte");
            Ordinal.NullableByte = record.GetOrdinal("nullable_byte");
            Ordinal.MyGuid = record.GetOrdinal("my_guid");
            Ordinal.NullableGuid = record.GetOrdinal("nullable_guid");
            Ordinal.IsGood = record.GetOrdinal("is_good");
            Ordinal.IsBad = record.GetOrdinal("is_bad");
            Ordinal.BirthDate = record.GetOrdinal("birth_date");
            Ordinal.LastSeen = record.GetOrdinal("last_seen");
            Ordinal.Name = record.GetOrdinal("name");
            Ordinal.MiddleName = record.GetOrdinal("middle_name");
            System.Threading.Thread.MemoryBarrier();
            Ordinal.__initialized__ = true;
        }
        return new Person
        {
            Id = record.GetInt32(Ordinal.Id),
            Age = record.IsDBNull(Ordinal.Age) ? null : record.GetInt32(Ordinal.Age),
            Float = record.GetFloat(Ordinal.Float),
            NullableFloat = record.IsDBNull(Ordinal.NullableFloat) ? null : record.GetFloat(Ordinal.NullableFloat),
            Double = record.GetDouble(Ordinal.Double),
            NullableDouble = record.IsDBNull(Ordinal.NullableDouble) ? null : record.GetDouble(Ordinal.NullableDouble),
            Short = record.GetInt16(Ordinal.Short),
            NullableShort = record.IsDBNull(Ordinal.NullableShort) ? null : record.GetInt16(Ordinal.NullableShort),
            Long = record.GetInt64(Ordinal.Long),
            NullableLong = record.IsDBNull(Ordinal.NullableLong) ? null : record.GetInt64(Ordinal.NullableLong),
            Char = record.GetChar(Ordinal.Char),
            NullableChar = record.IsDBNull(Ordinal.NullableChar) ? null : record.GetChar(Ordinal.NullableChar),
            Byte = record.GetByte(Ordinal.Byte),
            NullableByte = record.IsDBNull(Ordinal.NullableByte) ? null : record.GetByte(Ordinal.NullableByte),
            MyGuid = record.GetGuid(Ordinal.MyGuid),
            NullableGuid = record.IsDBNull(Ordinal.NullableGuid) ? null : record.GetGuid(Ordinal.NullableGuid),
            IsGood = record.GetBoolean(Ordinal.IsGood),
            IsBad = record.IsDBNull(Ordinal.IsBad) ? null : record.GetBoolean(Ordinal.IsBad),
            BirthDate = record.GetDateTime(Ordinal.BirthDate),
            LastSeen = record.IsDBNull(Ordinal.LastSeen) ? null : record.GetDateTime(Ordinal.LastSeen),
            Name = record.GetString(Ordinal.Name),
            MiddleName = record.GetString(Ordinal.MiddleName),
        };
    }
}
