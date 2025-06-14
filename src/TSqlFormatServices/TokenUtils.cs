using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace TSqlFormatServices;

internal static class TokenUtils
{
    // TODO: "NonTrivialToken" instead, skipping inline comments.
    public static int GetPreviousNonWhitespaceTokenIndexInside(TSqlFragment node, int beforeIndex)
    {
        for (var index = beforeIndex - 1; index >= node.FirstTokenIndex; index--)
        {
            if (node.ScriptTokenStream[index].TokenType != TSqlTokenType.WhiteSpace)
                return index;
        }

        return -1;
    }

    // TODO: "NonTrivialToken" instead, skipping inline comments.
    public static int GetNextNonWhitespaceTokenIndexInside(TSqlFragment node, int afterIndex)
    {
        for (var index = afterIndex + 1; index <= node.LastTokenIndex; index++)
        {
            if (node.ScriptTokenStream[index].TokenType != TSqlTokenType.WhiteSpace)
                return index;
        }

        return -1;
    }

    public static int FindIdentifierTokenIndexInside(TSqlFragment node, string identifierText, int startIndex, int endIndex)
    {
        for (var index = startIndex; index <= endIndex; index++)
        {
            if (node.ScriptTokenStream[index] is { TokenType: TSqlTokenType.Identifier } token
                && token.Text.Equals(identifierText, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return -1;
    }

    public static TSqlTokenType GetNextNonWhitespaceTokenType(IList<TSqlParserToken> stream, int afterIndex)
    {
        for (var index = afterIndex + 1; index < stream.Count; index++)
        {
            var tokenType = stream[index].TokenType;

            if (tokenType != TSqlTokenType.WhiteSpace)
                return tokenType;
        }

        return TSqlTokenType.None;
    }
}
