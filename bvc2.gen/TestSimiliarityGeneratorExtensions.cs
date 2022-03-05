using Microsoft.CodeAnalysis;
using System.Text;

namespace bvc2.gen;

static class TestSimiliarityGeneratorExtensions
{
    internal static StringBuilder AppendThrowLine(this StringBuilder sb, string? subFieldName = null) => subFieldName is null
        ? sb.AppendLine($"throw new AssertFailedException($\"Similarity assert failed on property {{fieldName}}.\\n\\nExpected: {{a}}\\nEncountered: {{b}}\");")
        : sb.AppendLine($"throw new AssertFailedException($\"Similarity assert failed on property {{fieldName.{subFieldName}}}.\\n\\nExpected: {{a.{subFieldName}}}\\nEncountered: {{b.{subFieldName}}}\");");

    internal static bool HasBaseType(this ITypeSymbol typeSymbol, string baseTypeName) =>
        typeSymbol.BaseType?.OriginalDefinition.ToDisplayString() == baseTypeName || (typeSymbol.BaseType?.HasBaseType(baseTypeName) ?? false);
}
