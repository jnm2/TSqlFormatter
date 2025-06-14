using System.Collections.Immutable;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace TSqlFormatServices;

internal sealed class FormattingVisitor(TSqlFormatter formatter) : TSqlFragmentVisitor
{
    public List<(int InsertIndex, int DeleteCount, ImmutableArray<TSqlParserToken> InsertedTokens)> Replacements { get; } = [];
    public List<(int StartIndex, int EndIndex)> Indentation { get; } = [];

    public override void Visit(BeginEndBlockStatement node)
    {
        if (formatter.SemicolonUsage is not null)
        {
            var beginTokenIndex = node.FirstTokenIndex;
            if (node.ScriptTokenStream[beginTokenIndex].TokenType != TSqlTokenType.Begin)
                throw new InvalidOperationException("Expected: BeginEndBlockStatement starts with a Begin token");

            var semicolonTokenIndex = TokenUtils.GetNextNonWhitespaceTokenIndexInside(node, afterIndex: beginTokenIndex);
            if (semicolonTokenIndex != -1 && node.ScriptTokenStream[semicolonTokenIndex].TokenType == TSqlTokenType.Semicolon)
            {
                if (formatter.SemicolonUsage != SemicolonUsage.Always)
                    Replacements.Add((semicolonTokenIndex, 1, []));
            }
            else
            {
                if (formatter.SemicolonUsage == SemicolonUsage.Always)
                    Replacements.Add((beginTokenIndex + 1, 0, [TokenFactory.Semicolon()]));
            }
        }

        base.Visit(node);
    }

    public override void Visit(TryCatchStatement node)
    {
        if (formatter.SemicolonUsage is not null)
        {
            var tryTokenIndex = TokenUtils.FindIdentifierTokenIndexInside(node, "TRY", startIndex: node.FirstTokenIndex, endIndex: node.TryStatements.FirstTokenIndex - 1);
            if (tryTokenIndex == -1)
                throw new InvalidOperationException("Cannot locate TRY token of TryCatchStatement");

            var semicolonTokenIndex = TokenUtils.GetNextNonWhitespaceTokenIndexInside(node, afterIndex: tryTokenIndex);
            if (semicolonTokenIndex != -1 && node.ScriptTokenStream[semicolonTokenIndex].TokenType == TSqlTokenType.Semicolon)
            {
                if (formatter.SemicolonUsage != SemicolonUsage.Always)
                    Replacements.Add((semicolonTokenIndex, 1, []));
            }
            else
            {
                if (formatter.SemicolonUsage == SemicolonUsage.Always)
                    Replacements.Add((tryTokenIndex + 1, 0, [TokenFactory.Semicolon()]));
            }

            var catchTokenIndex = TokenUtils.FindIdentifierTokenIndexInside(node, "CATCH", startIndex: node.TryStatements.LastTokenIndex + 1, endIndex: node.CatchStatements.FirstTokenIndex - 1);
            if (catchTokenIndex == -1)
                throw new InvalidOperationException("Cannot locate CATCH token of TryCatchStatement");

            semicolonTokenIndex = TokenUtils.GetNextNonWhitespaceTokenIndexInside(node, afterIndex: catchTokenIndex);
            if (semicolonTokenIndex != -1 && node.ScriptTokenStream[semicolonTokenIndex].TokenType == TSqlTokenType.Semicolon)
            {
                if (formatter.SemicolonUsage != SemicolonUsage.Always)
                    Replacements.Add((semicolonTokenIndex, 1, []));
            }
            else
            {
                if (formatter.SemicolonUsage == SemicolonUsage.Always)
                    Replacements.Add((catchTokenIndex + 1, 0, [TokenFactory.Semicolon()]));
            }
        }

        base.Visit(node);
    }

    public override void Visit(StatementList node)
    {
        for (var i = 1; i < node.Statements.Count; i++)
        {
            if (node.Statements[i] is SelectStatement { WithCtesAndXmlNamespaces.CommonTableExpressions: not [] })
                nextStatementsRequiringSemicolon.Add(node.Statements[i - 1]);
        }

        base.Visit(node);
    }

    private readonly List<TSqlStatement> nextStatementsRequiringSemicolon = [];

    public override void Visit(TSqlStatement node)
    {
        var nextStatementRequiresSemicolon = nextStatementsRequiringSemicolon.Remove(node);

        if (formatter.SemicolonUsage is not null && TSqlFacts.CanHaveOwnSemicolon(node))
        {
            if (node.ScriptTokenStream[node.LastTokenIndex].TokenType == TSqlTokenType.Semicolon)
            {
                if (formatter.SemicolonUsage == SemicolonUsage.OnlyWhenNecessary && !nextStatementRequiresSemicolon)
                    Replacements.Add((node.LastTokenIndex, 1, []));
            }
            else
            {
                if (formatter.SemicolonUsage != SemicolonUsage.OnlyWhenNecessary)
                    Replacements.Add((node.LastTokenIndex + 1, 0, [TokenFactory.Semicolon()]));
            }
        }

        base.Visit(node);
    }

    public override void Visit(SelectStatement node)
    {
        if (TSqlFacts.GetIntoClauseKeywordTokenIndex(node) is (not -1) and var intoKeywordTokenIndex)
            ReplaceWhitespaceWithNewLine(node.ScriptTokenStream, intoKeywordTokenIndex, before: true);

        base.Visit(node);
    }

    public override void Visit(SelectScalarExpression node)
    {
        ApplyAsKeywordPreference(node, node.ColumnName);
        base.Visit(node);
    }

    public override void Visit(QuerySpecification node)
    {
        if (node is not { SelectElements: [SelectStarExpression], WhereClause: null, GroupByClause: null, HavingClause: null, WindowClause: null, OrderByClause: null, OffsetClause: null })
        {
            ReplaceLeadingWhitespaceWithNewLine(node.FromClause);
            ReplaceLeadingWhitespaceWithNewLine(node.WhereClause);
            ReplaceLeadingWhitespaceWithNewLine(node.GroupByClause);
            ReplaceLeadingWhitespaceWithNewLine(node.HavingClause);

            if (TSqlFacts.GetWindowClauseKeywordTokenIndex(node) is (not -1) and var windowKeywordTokenIndex)
                ReplaceWhitespaceWithNewLine(node.ScriptTokenStream, windowKeywordTokenIndex, before: true);

            ReplaceLeadingWhitespaceWithNewLine(node.OrderByClause);
            ReplaceLeadingWhitespaceWithNewLine(node.OffsetClause);
        }

        base.Visit(node);
    }

    public override void Visit(TableReferenceWithAlias node)
    {
        ApplyAsKeywordPreference(node, node.Alias);
        base.Visit(node);
    }

    public override void Visit(QueryDerivedTable node)
    {
        if (!TSqlFacts.IsFirstNonWhitespaceOnLine(node.QueryExpression)
            || node.QueryExpression.StartColumn <= TSqlFacts.GetLineContentStartColumn(node.ScriptTokenStream, node.QueryExpression.StartLine - 1))
        {
            ReplaceLeadingWhitespaceWithNewLine(node.QueryExpression);
            Indentation.Add((node.QueryExpression.FirstTokenIndex, node.QueryExpression.LastTokenIndex));
        }

        ReplaceTrailingWhitespaceWithNewLine(node.QueryExpression);

        base.Visit(node);
    }

    public override void Visit(BinaryQueryExpression node)
    {
        ReplaceWhitespaceWithNewLine(node.ScriptTokenStream, node.FirstQueryExpression.LastTokenIndex, before: false);
        ReplaceWhitespaceWithNewLine(node.ScriptTokenStream, node.SecondQueryExpression.FirstTokenIndex, before: true);
        base.Visit(node);
    }

    public override void Visit(QualifiedJoin node)
    {
        ReplaceWhitespaceWithNewLine(node.ScriptTokenStream, node.FirstTableReference.LastTokenIndex, before: false);
        base.Visit(node);
    }

    public override void Visit(UnqualifiedJoin node)
    {
        ReplaceWhitespaceWithNewLine(node.ScriptTokenStream, node.FirstTableReference.LastTokenIndex, before: false);
        base.Visit(node);
    }

    private readonly List<Identifier> skipIdentifiers = [];

    public override void Visit(Identifier node)
    {
        if (!skipIdentifiers.Remove(node))
        {
            if (formatter.IdentifierQuoting is not null)
            {
                if (node.QuoteType == QuoteType.NotQuoted)
                {
                    if (formatter.IdentifierQuoting == IdentifierQuoting.Always)
                        Replacements.Add((node.FirstTokenIndex, DeleteCount: 1, [TokenFactory.Identifier(node.Value, QuoteType.SquareBracket)]));
                }
                else
                {
                    if (formatter.IdentifierQuoting == IdentifierQuoting.OnlyWhenNecessary
                        && !TSqlFacts.IdentifierRequiresQuoting(node.Value))
                    {
                        Replacements.Add((node.FirstTokenIndex, DeleteCount: 1, [TokenFactory.Identifier(node.Value, QuoteType.NotQuoted)]));
                    }
                }
            }
        }

        base.Visit(node);
    }

    public override void Visit(FunctionCall node)
    {
        if (TSqlFacts.IsBuiltInFunction(node.FunctionName.Value))
        {
            if (formatter.BuiltInFunctionCasing is not null)
            {
                var functionName = TSqlFacts.ApplyCasing(node.FunctionName.Value, formatter.BuiltInFunctionCasing);

                Replacements.Add((node.FunctionName.FirstTokenIndex, DeleteCount: 1, [TokenFactory.Identifier(functionName, node.FunctionName.QuoteType)]));
            }

            skipIdentifiers.Add(node.FunctionName);
        }

        base.Visit(node);
    }

    private void ReplaceLeadingWhitespaceWithNewLine(TSqlFragment? node)
    {
        if (node is not null)
            ReplaceWhitespaceWithNewLine(node.ScriptTokenStream, node.FirstTokenIndex, before: true);
    }

    private void ReplaceTrailingWhitespaceWithNewLine(TSqlFragment? node)
    {
        if (node is not null)
            ReplaceWhitespaceWithNewLine(node.ScriptTokenStream, node.LastTokenIndex, before: false);
    }

    private void ReplaceWhitespaceWithNewLine(IList<TSqlParserToken> stream, int atIndex, bool before)
    {
        var startingIndex = atIndex;
        if (!before) startingIndex++;

        var index = startingIndex;

        if (before)
        {
            while (index > 0 && stream[index - 1].TokenType == TSqlTokenType.WhiteSpace)
                index--;

            Replacements.Add((index, startingIndex - index, [TokenFactory.NewLine()]));
        }
        else
        {
            while (index < stream.Count && stream[index].TokenType == TSqlTokenType.WhiteSpace)
                index++;

            Replacements.Add((startingIndex, index - startingIndex, [TokenFactory.NewLine()]));
        }
    }

    private void ApplyAsKeywordPreference(TSqlFragment containingNode, TSqlFragment? identifierNode)
    {
        if (identifierNode is not null && formatter.UseAsKeywordForAliases is not null)
        {
            if (TokenUtils.GetPreviousNonWhitespaceTokenIndexInside(containingNode, beforeIndex: identifierNode.FirstTokenIndex) is (not -1) and var index
                && containingNode.ScriptTokenStream[index] is { TokenType: TSqlTokenType.As })
            {
                if (formatter.UseAsKeywordForAliases is false)
                    Replacements.Add((index, DeleteCount: 1, []));
            }
            else
            {
                if (formatter.UseAsKeywordForAliases is true)
                    Replacements.Add((identifierNode.FirstTokenIndex, DeleteCount: 0, [MakeKeyword(TSqlTokenType.As, containingNode), TokenFactory.Space()]));
            }
        }
    }

    private TSqlParserToken MakeKeyword(TSqlTokenType tokenType, TSqlFragment contextNode)
    {
        return TokenFactory.Keyword(tokenType, formatter.KeywordCasing ?? TSqlFacts.InferCasing(contextNode));
    }
}
