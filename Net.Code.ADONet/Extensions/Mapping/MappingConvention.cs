namespace Net.Code.ADONet;
public record struct MappingConvention(Func<string, string> ToDb, Func<string, string> FromDb, char Escape)
{
    /// <summary>
    /// Maps column names to property names based on exact, case sensitive match. Database artefacts are named exactly
    /// like the .Net objects.
    /// </summary>
    public static readonly MappingConvention Default = new(StringExtensions.NoOp, StringExtensions.NoOp, '@');

    public string Parameter(string s) => $"{Escape}{s}";
}