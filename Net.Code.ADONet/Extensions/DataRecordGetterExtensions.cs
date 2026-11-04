using System.Globalization;
#if NET8_0_OR_GREATER
using System.Runtime.CompilerServices;
#endif

namespace Net.Code.ADONet;

/// <summary>
/// Typed getters for types that IDataRecord has no getter for, or that providers report differently.
/// For example, Npgsql 10+ reports 'date' and 'time' columns as DateOnly/TimeOnly, while SqlClient,
/// Sqlite and MySql report them as DateTime/TimeSpan. Each getter accepts every common representation.
/// On a DbDataReader the value is read with GetFieldValue&lt;T&gt;, so providers can avoid boxing.
/// </summary>
public static class DataRecordGetterExtensions
{
    extension(IDataRecord record)
    {
#if NET6_0_OR_GREATER
        /// <summary>
        /// Gets the value of the specified column as a DateOnly. Reads DateOnly columns directly
        /// and converts DateTime columns.
        /// </summary>
        public DateOnly GetDateOnly(int i) => record.GetFieldType(i) == typeof(DateOnly)
            ? record.Read<DateOnly>(i)
            : DateOnly.FromDateTime(record.GetDateTime(i));

        /// <summary>
        /// Gets the value of the specified column as a TimeOnly. Reads TimeOnly columns directly
        /// and converts TimeSpan columns.
        /// </summary>
        public TimeOnly GetTimeOnly(int i) => record.GetFieldType(i) == typeof(TimeOnly)
            ? record.Read<TimeOnly>(i)
            : TimeOnly.FromTimeSpan(record.Read<TimeSpan>(i));
#endif

        /// <summary>
        /// Gets the value of the specified column as a TimeSpan. Reads TimeSpan columns directly
        /// and converts TimeOnly columns.
        /// </summary>
        public TimeSpan GetTimeSpan(int i) =>
#if NET6_0_OR_GREATER
            record.GetFieldType(i) == typeof(TimeOnly) ? record.Read<TimeOnly>(i).ToTimeSpan() :
#endif
            record.Read<TimeSpan>(i);

        /// <summary>
        /// Gets the value of the specified column as a DateTimeOffset. Reads DateTimeOffset columns
        /// directly and converts DateTime columns with the standard DateTime to DateTimeOffset
        /// conversion: UTC values get offset zero, local and unspecified values the local offset.
        /// </summary>
        public DateTimeOffset GetDateTimeOffset(int i) => record.GetFieldType(i) == typeof(DateTime)
            ? new DateTimeOffset(record.GetDateTime(i))
            : record.Read<DateTimeOffset>(i);

        /// <summary>
        /// Gets the value of the specified column as a byte array.
        /// </summary>
        public byte[] GetByteArray(int i) => record.Read<byte[]>(i);

        /// <summary>
        /// Gets the value of the specified column as an enum. String columns are parsed by name
        /// (case-insensitive); integer columns of any width are converted by value.
        /// </summary>
        public TEnum GetEnum<TEnum>(int i) where TEnum : struct, Enum => record.GetFieldType(i) == typeof(string)
            ? Enum.Parse<TEnum>(record.GetString(i), ignoreCase: true)
            : FromInt64<TEnum>(record.ReadInt64(i));

        // DbDataReader.GetFieldValue<T> is generic, so providers can return the value without boxing;
        // a plain IDataRecord only offers GetValue.
        private T Read<T>(int i) => record is DbDataReader reader ? reader.GetFieldValue<T>(i) : (T)record.GetValue(i);

        // reads an integer column of any width (or a decimal, e.g. Oracle NUMBER) without boxing
        private long ReadInt64(int i)
        {
            var type = record.GetFieldType(i);
            return type == typeof(long) ? record.GetInt64(i)
                : type == typeof(int) ? record.GetInt32(i)
                : type == typeof(short) ? record.GetInt16(i)
                : type == typeof(byte) ? record.GetByte(i)
                : type == typeof(decimal) ? (long)record.GetDecimal(i)
                : Convert.ToInt64(record.GetValue(i), CultureInfo.InvariantCulture);
        }
    }

    // Same result as an explicit (TEnum) cast of an integer. On .NET 8+ this does not box:
    // the JIT reduces the switch to the single branch for the enum's size.
    private static TEnum FromInt64<TEnum>(long value) where TEnum : struct, Enum
    {
#if NET8_0_OR_GREATER
        return Unsafe.SizeOf<TEnum>() switch
        {
            1 => Unsafe.BitCast<byte, TEnum>((byte)value),
            2 => Unsafe.BitCast<short, TEnum>((short)value),
            4 => Unsafe.BitCast<int, TEnum>((int)value),
            _ => Unsafe.BitCast<long, TEnum>(value)
        };
#else
        return (TEnum)Enum.ToObject(typeof(TEnum), value);
#endif
    }
}
