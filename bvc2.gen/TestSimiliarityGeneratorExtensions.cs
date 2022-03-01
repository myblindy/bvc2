using System.Text;

namespace bvc2.gen;

static class TestSimiliarityGeneratorExtensions
{
    internal static StringBuilder AppendThrowLine(this StringBuilder sb) =>
        sb.AppendLine($"throw new AssertFailedException($\"Similarity assert failed on property {{fieldName}}.\\n\\nExpected: {{a}}\\nEncountered: {{b}}\");");
}
