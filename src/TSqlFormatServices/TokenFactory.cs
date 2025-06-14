using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace TSqlFormatServices;

internal static class TokenFactory
{
    public static TSqlParserToken Keyword(TSqlTokenType tokenType, KeywordCasing? casing)
    {
        var token = new TSqlParserToken(tokenType, TSqlFacts.ApplyCasing(tokenType.ToString(), casing));

        if (!token.IsKeyword())
            throw new ArgumentException("The provided token type is not a keyword.", nameof(tokenType));

        return token;
    }

    public static TSqlParserToken Space() => new(TSqlTokenType.WhiteSpace, " ");

    public static TSqlParserToken NewLine() => new(TSqlTokenType.WhiteSpace, Environment.NewLine);

    public static TSqlParserToken Identifier(string value, QuoteType quoteType)
    {
        return new TSqlParserToken(TSqlTokenType.Identifier, quoteType switch
        {
            QuoteType.NotQuoted => value,
            QuoteType.SquareBracket => '[' + value.Replace("]", "]]") + ']',
            QuoteType.DoubleQuote => '"' + value.Replace("\"", "\"\"") + '"',
        });
    }

    public static TSqlParserToken Semicolon() => new(TSqlTokenType.Semicolon, ";");
}
