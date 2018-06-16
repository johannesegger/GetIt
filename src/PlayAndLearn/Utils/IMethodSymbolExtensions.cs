using System.Reflection;
using Microsoft.CodeAnalysis;

namespace PlayAndLearn.Utils
{
    public static class IMethodSymbolExtensions
    {
        public static MethodInfo FindInAssembly(this IMethodSymbol methodSymbol, Assembly assembly)
        {
            var typeName = string.IsNullOrEmpty(methodSymbol.ContainingNamespace.MetadataName)
                ? methodSymbol.ContainingType.MetadataName
                : $"{methodSymbol.ContainingNamespace.MetadataName}.{methodSymbol.ContainingType.MetadataName}";
            return assembly
                .GetType(typeName)
                .GetTypeInfo()
                .GetDeclaredMethod(methodSymbol.MetadataName);
        }
    }
}