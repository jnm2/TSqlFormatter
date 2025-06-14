using System.Collections.Immutable;
using System.Text;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace TSqlFormatServices;

public sealed class TSqlFormatter
{
    public KeywordCasing? KeywordCasing { get; set; }
    public KeywordCasing? BuiltInFunctionCasing { get; set; }
    public bool? UseAsKeywordForAliases { get; set; }
    public bool NormalizeConsecutiveSpaces { get; set; }
    public IdentifierQuoting? IdentifierQuoting { get; set; }
    public SemicolonUsage? SemicolonUsage { get; set; }

    public string Format(string input)
    {
        using var writer = new StringWriter();
        using var reader = new StringReader(input);

        var parser = new TSql160Parser(initialQuotedIdentifiers: true);

        var statementList = parser.ParseStatementList(reader, out var errors);
        if (statementList is null)
        {
            throw new InvalidOperationException(
                "Failed to parse the text from the reader as a SQL fragment:" + string.Concat(errors.Select(e =>
                    Environment.NewLine + e.Message)));
        }

        Format(statementList, writer);

        return writer.ToString();
    }

    public void Format(TSqlFragment node, TextWriter writer)
    {
        var replacementsVisitor = new FormattingVisitor(this);
        node.Accept(replacementsVisitor);

        var replacements = new Queue<(int InsertIndex, int DeleteCount, ImmutableArray<TSqlParserToken> InsertedTokens)>(replacementsVisitor.Replacements.OrderBy(r => r.InsertIndex));

        var indentationChanges = new Queue<(int Index, bool Increase)>(replacementsVisitor.Indentation
            .SelectMany(i => new[]
            {
                (Index: i.StartIndex, Increase: true),
                (Index: i.EndIndex + 1, Increase: false),
            })
            .OrderBy(i => i.Index));

        var tokenIndex = 0;
        var indentationLevel = 0;
        var isAtStartOfLine = true;
        var bufferedSameLineWhitespace = new StringBuilder();

        while (true)
        {
            if (indentationChanges.TryPeek(out var indentationChange) && indentationChange.Index <= tokenIndex)
            {
                if (indentationChange.Increase)
                    indentationLevel++;
                else
                    indentationLevel--;
                indentationChanges.Dequeue();
            }

            if (replacements.TryPeek(out var replacement) && replacement.InsertIndex <= tokenIndex)
            {
                if (replacement.InsertIndex < tokenIndex)
                    throw new InvalidOperationException("Replacements overlap.");

                foreach (var token in replacement.InsertedTokens)
                    WriteToken(token);

                tokenIndex += replacement.DeleteCount;
                replacements.Dequeue();
            }
            else if (tokenIndex < node.ScriptTokenStream.Count)
            {
                WriteToken(node.ScriptTokenStream[tokenIndex]);
                tokenIndex++;
            }
            else
            {
                break;
            }
        }

        void WriteToken(TSqlParserToken token)
        {
            if (token.TokenType == TSqlTokenType.WhiteSpace)
            {
                if (token.Text is "\n" or "\r\n")
                {
                    isAtStartOfLine = true;
                    bufferedSameLineWhitespace.Clear();
                    writer.Write(token.Text);
                }
                else
                {
                    if (token.Text.IndexOfAny(['\r', '\n']) != -1)
                        throw new InvalidOperationException("A whitespace token must either be a single line ending or must not contain any line endings.");

                    if (NormalizeConsecutiveSpaces)
                    {
                        if (bufferedSameLineWhitespace.Length == 0)
                            bufferedSameLineWhitespace.Append(' ');
                    }
                    else
                    {
                        bufferedSameLineWhitespace.Append(token.Text);
                    }
                }
            }
            else if (token.TokenType != TSqlTokenType.EndOfFile)
            {
                if (isAtStartOfLine)
                {
                    writer.Write(new string(' ', indentationLevel * 4));
                    isAtStartOfLine = false;
                }

                writer.Write(bufferedSameLineWhitespace);
                bufferedSameLineWhitespace.Clear();

                writer.Write(token.IsKeyword()
                    ? TSqlFacts.ApplyCasing(token.Text, KeywordCasing)
                    : token.Text);
            }
        }
    }
}
