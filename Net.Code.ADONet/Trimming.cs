namespace Net.Code.ADONet
{
    internal static class Trimming
    {
        public const string ReflectionMessage =
            "Maps properties of the given type using reflection; they may be trimmed. "
            + "Use the source generator ([MapFromDataRecord]) and Select(Func<IDataRecord, T>) for trimmed or AOT applications.";
        public const string DynamicCodeMessage =
            "Creates delegates for generic methods over property types at runtime. "
            + "Use the source generator ([MapFromDataRecord]) and Select(Func<IDataRecord, T>) for AOT applications.";
        public const string DynamicMessage =
            "Uses 'dynamic', which requires runtime code generation.";
    }
}

#if !NET7_0_OR_GREATER
// Polyfills, so the attributes can be used on all target frameworks. The trimming and AOT
// analyzers only run for the net8.0 target, which has the real attributes.
namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class, Inherited = false)]
    internal sealed class RequiresUnreferencedCodeAttribute(string message) : Attribute
    {
        public string Message { get; } = message;
        public string? Url { get; set; }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class, Inherited = false)]
    internal sealed class RequiresDynamicCodeAttribute(string message) : Attribute
    {
        public string Message { get; } = message;
        public string? Url { get; set; }
    }

    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    internal sealed class UnconditionalSuppressMessageAttribute(string category, string checkId) : Attribute
    {
        public string Category { get; } = category;
        public string CheckId { get; } = checkId;
        public string? Justification { get; set; }
    }
}
#endif
