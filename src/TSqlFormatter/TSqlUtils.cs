using Microsoft.SqlServer.TransactSql.ScriptDom;
using TSqlFormatServices;

namespace TSqlFormatter;

public static class TSqlUtils
{
    public static (string Oldest, string Newest)? GetSupportedProductRange(string sqlText, bool newestVersionSucceeded)
    {
        var newestWorkingIndex = newestVersionSucceeded
            ? SqlVersionUtils.SqlServerVersions.Length - 1
            : SqlVersionUtils.SqlServerVersions.AsSpan(..^1).FindLastIndex(v => ParsesWithSqlVersion(sqlText, v.SqlVersion));

        if (newestWorkingIndex == -1)
            return null;

        var newestWorkingVersion = SqlVersionUtils.SqlServerVersions[newestWorkingIndex].ProductVersion;

        var oldestWorkingIndex = SqlVersionUtils.SqlServerVersions.AsSpan(..newestWorkingIndex)
            .BisectIndex(containsChange: v => ParsesWithSqlVersion(sqlText, v.SqlVersion));

        return (
            oldestWorkingIndex == -1
                ? newestWorkingVersion
                : SqlVersionUtils.SqlServerVersions[oldestWorkingIndex].ProductVersion,
            newestWorkingVersion);
    }

    public static bool ParsesWithSqlVersion(string sqlText, SqlVersion sqlVersion)
    {
        var parser = TSqlParser.CreateParser(sqlVersion, initialQuotedIdentifiers: true);
        using var reader = new StringReader(sqlText);
        _ = parser.Parse(reader, out var errors);
        return errors is null or [];
    }
}
