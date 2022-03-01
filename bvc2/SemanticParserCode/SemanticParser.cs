﻿using bvc2.SyntaxParserCode;

namespace bvc2.SemanticParserCode;

internal class SemanticParser
{
    readonly RootSyntaxNode rootSyntaxNode;

    public SemanticParser(RootSyntaxNode rootSyntaxNode) =>
        this.rootSyntaxNode = rootSyntaxNode;

    [return: NotNullIfNotNull("expressionSyntaxNode")]
    static SemanticExpression? GetSemanticExpression(ExpressionSyntaxNode? expressionSyntaxNode) =>
        expressionSyntaxNode switch
        {
            null => null,
            BinaryExpressionSyntaxNode binaryExpressionSyntaxNode => new BinarySemanticExpression(
                GetSemanticExpression(binaryExpressionSyntaxNode.Left), binaryExpressionSyntaxNode.Operator, GetSemanticExpression(binaryExpressionSyntaxNode.Right)),
            LiteralExpressionSyntaxNode literalExpressionSyntaxNode => new LiteralSemanticExpression(literalExpressionSyntaxNode.Value),
            _ => throw new NotImplementedException(),
        };

    static void ParseEnumSyntaxNode(EnumSyntaxNode node, SemanticEntry parent)
    {
        var enumEntry = new EnumSemanticEntry(node.Name);
        parent.Children.Add(enumEntry);

        foreach (VariableSyntaxNode child in node.Children)
            enumEntry.Children.Add(new VariableSemanticEntry(child.Modifiers, child.Name, enumEntry, new LiteralSemanticExpression(GetSemanticExpression(child.InitialValue)!)));
    }

    static void ParseClassSyntaxNode(ClassSyntaxNode node, SemanticEntry parent)
    {
        var classEntry = new ClassSemanticEntry(node.Name, node.GenericTypes ?? Array.Empty<string>());
        parent.Children.Add(classEntry);

        ParseChildren(node, classEntry);
    }

    static void ParseVariableSyntaxNode(VariableSyntaxNode node, SemanticEntry parent) =>
        parent.Children.Add(new VariableSemanticEntry(node.Modifiers, node.Name, parent.FindType(node.ReturnType!), GetSemanticExpression(node.InitialValue)));

    static void ParseChildren(ParentSyntaxNode node, SemanticEntry parent)
    {
        foreach (var child in node.Children)
            switch (child)
            {
                case EnumSyntaxNode enumSyntaxNode:
                    ParseEnumSyntaxNode(enumSyntaxNode, parent);
                    break;
                case ClassSyntaxNode classSyntaxNode:
                    ParseClassSyntaxNode(classSyntaxNode, parent);
                    break;
                case VariableSyntaxNode variableSyntaxNode:
                    ParseVariableSyntaxNode(variableSyntaxNode, parent);
                    break;
                default:
                    throw new NotImplementedException();
            }
    }

    static readonly ClassSemanticEntry integerSemanticEntry = new ClassSemanticEntry("Integer", Array.Empty<string>())
    {
        Internal = true,
        Children =
        {

        }
    };
    static readonly ClassSemanticEntry doubleSemanticEntry = new ClassSemanticEntry("Double", Array.Empty<string>())
    {
        Internal = true,
        Children =
        {

        }
    };
    static void AppendRuntimeTypeSemanticEntries(RootSemanticEntry root)
    {
        root.Children.Add(integerSemanticEntry);
        root.Children.Add(doubleSemanticEntry);
    }

    public SemanticEntry Parse()
    {
        var result = new RootSemanticEntry();
        AppendRuntimeTypeSemanticEntries(result);
        ParseChildren(rootSyntaxNode, result);
        return result;
    }
}
