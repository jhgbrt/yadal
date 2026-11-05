namespace Net.Code.ADONet;
public record struct MappingConvention(Func<string, string> ToDb, Func<string, string> FromDb, char Escape)
{
    /// <summary>
    /// Maps column names to property names by name, without transformation: database artefacts are named like
    /// the .Net objects. Columns are matched exactly first, then case-insensitively.
    /// </summary>
    public static readonly MappingConvention Default = new(StringExtensions.NoOp, StringExtensions.NoOp, '@');

    public string Parameter(string s) => $"{Escape}{s}";
}