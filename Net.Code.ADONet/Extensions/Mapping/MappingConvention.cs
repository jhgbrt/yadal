namespace Net.Code.ADONet.Extensions.Mapping;
internal record struct MappingConvention(Func<string, string> ToDb, Func<string, string> FromDb, char Escape) : IMappingConvention
{
    /// <summary>
    /// Maps column names to property names based on exact, case sensitive match. Database artefacts are named exactly
    /// like the .Net objects.
    /// </summary>
    public static readonly IMappingConvention Default
        = new MappingConvention(NoOp, NoOp, '@');

    static string NoOp(string s) => s;

    public string Parameter(string s) => $"{Escape}{s}";

    string IMappingConvention.FromDb(string s) => FromDb(s);

    string IMappingConvention.ToDb(string s) => ToDb(s);
}