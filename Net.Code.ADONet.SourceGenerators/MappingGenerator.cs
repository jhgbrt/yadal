using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Net.Code.ADONet.SourceGenerators
{
    [Generator]
    public class MappingGenerator : IIncrementalGenerator
    {
        const string AttributeName = "Net.Code.ADONet.MapFromDataRecordAttribute";
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var types = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    AttributeName,
                    predicate: (node, _) => node is ClassDeclarationSyntax or RecordDeclarationSyntax,
                    transform: GetMapperInfo
                )
                .Where(t => t is not null)
                .Collect()
                .SelectMany((x,_) => x.Distinct());

            context.RegisterSourceOutput(types, GenerateCode);
        }


        private static MapperInfo GetMapperInfo(
            GeneratorAttributeSyntaxContext context,
            CancellationToken cancellationToken)
        {
            var type = (INamedTypeSymbol)context.TargetSymbol;
            var mapper = new MapperInfo(type);
            return mapper;
        }
      
        internal static readonly DiagnosticDescriptor UnsupportedPropertyType = new(
            id: "ADONET001",
            title: "Unsupported property type",
            messageFormat: "Property '{0}' of type '{1}' cannot be mapped from a data record; no mapper is generated for '{2}'",
            category: "Net.Code.ADONet",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        private static void GenerateCode(SourceProductionContext context, MapperInfo mapper)
        {
            var unsupported = mapper.Properties.Where(p => p.Getter is null).ToList();
            foreach (var p in unsupported)
            {
                context.ReportDiagnostic(Diagnostic.Create(UnsupportedPropertyType, p.Location, p.Name, p.TypeName, mapper.Name));
            }
            if (unsupported.Count > 0) return;

            var result = SourceGenerationHelper.GenerateExtensionClass(mapper);
            context.AddSource(result.HintName, result.Text);
        }
    }

  

}