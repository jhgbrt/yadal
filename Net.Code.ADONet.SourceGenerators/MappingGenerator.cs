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
                    predicate: (node, _) => node is ClassDeclarationSyntax,
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
      
        private static void GenerateCode(SourceProductionContext context, MapperInfo mapper)
        {
            var result = SourceGenerationHelper.GenerateExtensionClass(mapper);
            context.AddSource(result.HintName, result.Text);
        }
    }

  

}