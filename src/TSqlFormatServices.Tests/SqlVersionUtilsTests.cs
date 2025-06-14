using Microsoft.SqlServer.TransactSql.ScriptDom;
using System.Globalization;

namespace TSqlFormatServices.Tests;

public static class SqlVersionUtilsTests
{
    [Test]
    public static void No_product_versions_are_missing_or_out_of_order_for_all_compatibility_levels()
    {
        var versionsDefinedByEnum = new List<(int CompatibilityLevel, SqlVersion Version)>();

        foreach (var version in Enum.GetValues<SqlVersion>())
        {
            var valueName = version.ToString().AsSpan();

            if (valueName.StartsWith("Sql", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(valueName["Sql".Length..], NumberStyles.None, CultureInfo.InvariantCulture, out var compatibilityLevel))
            {
                versionsDefinedByEnum.Add((compatibilityLevel, version));
            }
        }

        SqlVersionUtils.SqlServerVersions.Select(v => v.SqlVersion).ShouldBe(
            from v in versionsDefinedByEnum
            orderby v.CompatibilityLevel
            select v.Version);
    }
}
