using bvc2.SyntaxParserCode;

namespace bvc2.SemanticParserCode;

internal class SemanticParser
{
    readonly RootSyntaxNode rootSyntaxNode;

    public SemanticParser(RootSyntaxNode rootSyntaxNode)
    {
        this.rootSyntaxNode = rootSyntaxNode;
    }

    public SemanticEntry Parse()
    {
        SemanticExpression GetSemanticExpression(ExpressionSyntaxNode expressionSyntaxNode) =>
            expressionSyntaxNode switch
            {
                LiteralExpressionSyntaxNode literalExpressionSyntaxNode => new LiteralSemanticExpression(literalExpressionSyntaxNode.Value),
                _ => throw new NotImplementedException(),
            };

        void ParseEnumSyntaxNode(EnumSyntaxNode node, SemanticEntry parent)
        {
            var enumEntry = new EnumSemanticEntry(node.Name);
            parent.Children.Add(enumEntry);

            foreach (VariableSyntaxNode child in node.Children)
                enumEntry.Children.Add(new VariableSemanticEntry(child.Modifiers, child.Name, enumEntry, new LiteralSemanticExpression(GetSemanticExpression(child.InitialValue!))));
        }

        void ParseChildren(ParentSyntaxNode node, SemanticEntry parent)
        {
            foreach (var child in node.Children)
                if (child is EnumSyntaxNode enumSyntaxNode)
                    ParseEnumSyntaxNode(enumSyntaxNode, parent);
                else
                    throw new NotImplementedException();
        }

        var result = new RootSemanticEntry();
        ParseChildren(rootSyntaxNode, result);
        return result;
    }
}
