using System.Text.RegularExpressions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace TSqlFormatServices;

internal static partial class TSqlFacts
{
    public static int GetIntoClauseKeywordTokenIndex(SelectStatement node)
    {
        if (node.Into is null)
            return -1;

        var index = TokenUtils.GetPreviousNonWhitespaceTokenIndexInside(node, beforeIndex: node.Into.FirstTokenIndex);
        if (node.ScriptTokenStream[index] is not { TokenType: TSqlTokenType.Into })
            throw new InvalidOperationException("Unable to locate the INTO keyword token of the INTO clause.");

        return index;
    }

    public static int GetWindowClauseKeywordTokenIndex(QuerySpecification node)
    {
        if (node.WindowClause is null)
            return -1;

        // Do an extra check due to the potential danger of finding a prior identifier named 'window' if WindowClause.FirstTokenIndex should change to point to the keyword in the future.
        if (TokenUtils.GetNextNonWhitespaceTokenType(node.ScriptTokenStream, node.WindowClause.FirstTokenIndex) != TSqlTokenType.As)
            throw new NotImplementedException("WindowClause.FirstTokenIndex no longer points at the window name token instead of the WINDOW keyword.");

        var index = TokenUtils.GetPreviousNonWhitespaceTokenIndexInside(node, beforeIndex: node.WindowClause.FirstTokenIndex);

        if (!(node.ScriptTokenStream[index] is { TokenType: TSqlTokenType.Identifier } windowKeywordToken
              && windowKeywordToken.Text.Equals("WINDOW", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Unable to locate the WINDOW keyword token of the WINDOW clause.");
        }

        return index;
    }

    public static string ApplyCasing(string keyword, KeywordCasing? keywordCasing)
    {
        return keywordCasing switch
        {
            KeywordCasing.Uppercase => keyword.ToUpperInvariant(),
            KeywordCasing.Lowercase => keyword.ToLowerInvariant(),
            KeywordCasing.PascalCase => char.ToUpperInvariant(keyword[0]) + keyword[1..].ToLowerInvariant(),
            _ => keyword,
        };
    }

    public static KeywordCasing? InferCasing(TSqlFragment contextNode)
    {
        var nearestKeyword =
            contextNode.ScriptTokenStream.Skip(contextNode.FirstTokenIndex).Take((contextNode.LastTokenIndex + 1) - contextNode.FirstTokenIndex)
                .Concat(contextNode.ScriptTokenStream.Take(contextNode.FirstTokenIndex).Reverse())
                .Concat(contextNode.ScriptTokenStream.Skip(contextNode.LastTokenIndex + 1))
                .FirstOrDefault(node => node.IsKeyword());

        if (nearestKeyword is not null)
        {
            foreach (var casing in Enum.GetValues<KeywordCasing>())
            {
                if (nearestKeyword.Text == ApplyCasing(nearestKeyword.Text, casing))
                    return casing;
            }
        }

        return null;
    }

    [GeneratedRegex(@"\A[\p{L}_#][\p{L}\p{Nd}@$#_]*\Z")]
    private static partial Regex ValidUnquotedIdentifier();

    public static bool IdentifierRequiresQuoting(string value)
    {
        return !ValidUnquotedIdentifier().IsMatch(value)
            || (Enum.TryParse<TSqlTokenType>(value, ignoreCase: true, out var tokenType)
                && new TSqlParserToken(tokenType, null).IsKeyword());
    }

    public static bool CanHaveOwnSemicolon(TSqlStatement statement)
    {
        return statement is not (IfStatement or WhileStatement);
    }

    public static bool IsFirstNonWhitespaceOnLine(QueryExpression node)
    {
        var index = node.FirstTokenIndex;

        while (true)
        {
            if (node.ScriptTokenStream[index].Column == 1)
                return true;

            index--;
            if (node.ScriptTokenStream[index].TokenType != TSqlTokenType.WhiteSpace)
                return false;
        }
    }

    internal static int GetLineContentStartColumn(IList<TSqlParserToken> stream, int lineNumber)
    {
        var index = 0;

        while (true)
        {
            if (index == stream.Count)
                throw new ArgumentOutOfRangeException(nameof(lineNumber), lineNumber, "Line number does not exist in the script token stream.");

            if (stream[index].Line == lineNumber)
                break;

            index++;
        }

        while (stream[index].TokenType == TSqlTokenType.WhiteSpace)
        {
            index++;

            if (index == stream.Count || stream[index].Line != lineNumber)
                return 1; // Don't count whitespace-only lines as indentation
        }

        return stream[index].Column;
    }
}
