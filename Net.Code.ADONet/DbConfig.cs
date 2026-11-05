using System.Diagnostics.CodeAnalysis;

namespace Net.Code.ADONet;

/// <summary>
/// <para>
/// The DbConfig class allows to configure database specific behaviour at runtime, without a direct
/// dependency on the underlying ADO.Net provider. It does 2 things
/// </para>
/// <para>
/// - provides a hook to configure a DbCommand in case some specific configuration is required. For example,
///   Oracle requires the BindByName property to be set to true for named parameters to work.
/// - Sets the way database naming conventions are mapped to .Net naming conventions. For example, in Oracle,
///   database and column names are upper case and separated by underscores. Postgres defaults to lower case.
///   This includes also the escape character that indicates parameter names in queries with named parameters.
/// </para>
/// </summary>
public class DbConfig(Action<IDbCommand> prepareCommand, MappingConvention mappingConvention)
{
    // declared first: static fields are initialized in textual order, and the configurations below use it
    private static readonly Action<IDbCommand> NoOp = _ => { };

    internal Action<IDbCommand> PrepareCommand => prepareCommand;
    internal MappingConvention MappingConvention => mappingConvention;

    /// <summary>
    /// True if commands need no provider-specific preparation. Only then the CRUD helpers use
    /// <c>DbBatch</c>, because batch commands cannot be prepared the same way.
    /// </summary>
    internal bool HasNoCommandPreparation => prepareCommand == NoOp;

    public static DbConfig FromProviderName(string providerName) => providerName switch
    {
        string s when s.StartsWith("Oracle") => Oracle,
        string s when s.StartsWith("Npgsql") => PostGreSQL,
        string s when s.StartsWith("IBM") => DB2,
        _ => Default
    };

    public static DbConfig FromProviderFactory(DbProviderFactory factory)
        => FromProviderName(factory.GetType().FullName ?? factory.GetType().Name);

    // By default, the Oracle driver does not support binding parameters by name;
    // one has to set the BindByName property on the OracleDbCommand.
    // Mapping: 
    // Oracle convention is to work with UPPERCASE_AND_UNDERSCORE instead of BookTitleCase
    public static readonly DbConfig Oracle
        = new (
            SetBindByName,
            new MappingConvention(StringExtensions.ToUpperWithUnderscores, StringExtensions.ToPascalCase, ':')
            );
    public static readonly DbConfig DB2
        = new (
            NoOp,
            new MappingConvention(StringExtensions.ToUpperWithUnderscores, StringExtensions.ToPascalCase, '@')
            );
    public static readonly DbConfig PostGreSQL
        = new (
            NoOp,
            new MappingConvention(StringExtensions.ToLowerWithUnderscores, StringExtensions.ToPascalCase, '@')
            );
    public static readonly DbConfig Default
        = new (
            NoOp,
            new MappingConvention(StringExtensions.NoOp, StringExtensions.NoOp, '@')
            );

    private static readonly ConcurrentDictionary<Type, PropertyInfo?> BindByNameProperties = new();

    [UnconditionalSuppressMessage("Trimming", "IL2070",
        Justification = "BindByName is a public property of the provider's command type (OracleCommand). If it is trimmed, it is not set.")]
    private static void SetBindByName(IDbCommand c)
        => BindByNameProperties
            .GetOrAdd(c.GetType(), t => t.GetProperty("BindByName", typeof(bool)) is { CanWrite: true } p ? p : null)
            ?.SetValue(c, true);
}
