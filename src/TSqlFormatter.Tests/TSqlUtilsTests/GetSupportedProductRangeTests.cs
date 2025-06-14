using TSqlFormatServices;

namespace TSqlFormatter.Tests.TSqlUtilsTests;

public static class GetSupportedProductRangeTests
{
    private static readonly string Oldest = SqlVersionUtils.SqlServerVersions.First().ProductVersion;
    private static readonly string Newest = SqlVersionUtils.SqlServerVersions.Last().ProductVersion;

    private static (string Oldest, string Newest)? GetSupportedProductRange(string sqlText)
    {
        var newestVersionSucceeded = TSqlUtils.ParsesWithSqlVersion(sqlText, SqlVersionUtils.SqlServerVersions.Last().SqlVersion);

        return TSqlUtils.GetSupportedProductRange(sqlText, newestVersionSucceeded);
    }

    [Test]
    public static void Fully_supported_syntax()
    {
        GetSupportedProductRange("select 1").ShouldBe((Oldest, Newest));
    }

    [Test]
    public static void Syntax_supported_through_2000()
    {
        GetSupportedProductRange("create table A (pivot int)").ShouldBe((Oldest, "2000"));
    }

    [Test]
    public static void Syntax_supported_through_2005()
    {
        GetSupportedProductRange("create table A (merge int)").ShouldBe((Oldest, "2005"));
    }

    [Test]
    public static void Syntax_supported_from_2005()
    {
        GetSupportedProductRange("create table A (precision int)").ShouldBe(("2005", Newest));
    }

    [Test]
    public static void Intersection_results_in_support_in_2005_only()
    {
        GetSupportedProductRange("create table A (merge int, precision int)").ShouldBe(("2005", "2005"));
    }

    [Test]
    public static void Syntax_supported_from_2012()
    {
        GetSupportedProductRange("select next value for my_seq").ShouldBe(("2012", Newest));
    }

    [Test]
    public static void Syntax_supported_from_2016()
    {
        GetSupportedProductRange("select somefunc(A) within group (order by B)").ShouldBe(("2016", Newest));
    }

    [Test]
    public static void Syntax_supported_from_2019()
    {
        GetSupportedProductRange("create function dbo.MyFunc() returns table with inline = on as return select 1;").ShouldBe(("2019", Newest));
    }

    [Test]
    public static void Syntax_supported_from_2022()
    {
        GetSupportedProductRange("select A from B window C as (order by A)").ShouldBe(("2022", Newest));
    }
}
