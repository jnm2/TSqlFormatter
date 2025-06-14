using System.Collections.Immutable;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace TSqlFormatServices;

public static class SqlVersionUtils
{
    public static ImmutableArray<(SqlVersion SqlVersion, string ProductVersion)> SqlServerVersions { get; } =
    [
        (SqlVersion.Sql80, "2000"),
        (SqlVersion.Sql90, "2005"),
        (SqlVersion.Sql100, "2008"),
        (SqlVersion.Sql110, "2012"),
        (SqlVersion.Sql120, "2014"),
        (SqlVersion.Sql130, "2016"),
        (SqlVersion.Sql140, "2017"),
        (SqlVersion.Sql150, "2019"),
        (SqlVersion.Sql160, "2022"),
        (SqlVersion.Sql170, "2025"),
    ];
}
