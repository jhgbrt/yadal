namespace Net.Code.ADONet;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class MapFromDataRecordAttribute : Attribute 
{
    public NamingConvention ColumnNamingConvention { get; set; } = NamingConvention.PascalCase;
}

public enum NamingConvention
{
    PascalCase = 0,
    lowercase = 1,
    UPPERCASE = 2
}
