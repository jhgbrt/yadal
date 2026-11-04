#if NET6_0_OR_GREATER
namespace Net.Code.ADONet;

/// <summary>
/// Typed readers for DateOnly and TimeOnly columns. Some providers (e.g. Npgsql 10+) report 'date'
/// and 'time' columns as DateOnly/TimeOnly, others (SqlClient, Sqlite, MySql, ...) as DateTime/TimeSpan.
/// These methods read either representation without boxing when the record is a DbDataReader.
/// </summary>
public static class DateAndTimeOnlyExtensions
{
    extension(IDataRecord record)
    {
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

        // DbDataReader.GetFieldValue<T> is generic, so providers can return the value without boxing;
        // a plain IDataRecord only offers GetValue (IDataRecord has no typed TimeSpan getter).
        private T Read<T>(int i) => record is DbDataReader reader ? reader.GetFieldValue<T>(i) : (T)record.GetValue(i);
    }
}
#endif
