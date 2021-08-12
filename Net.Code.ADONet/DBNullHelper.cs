namespace Net.Code.ADONet;

public static class DBNullHelper
{
    public static bool IsNull(object? o) => o == null || DBNull.Value.Equals(o);
    public static object? FromDb(object? o) => IsNull(o) ? null : o;
    public static object? ToDb(object? o) => IsNull(o) ? DBNull.Value : o;
}