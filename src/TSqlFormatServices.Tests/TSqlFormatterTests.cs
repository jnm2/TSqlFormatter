using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace TSqlFormatServices.Tests;

public static class TSqlFormatterTests
{
    private static string Format(string input, TSqlFormatter? formatter = null)
    {
        formatter ??= new();
        var result = formatter.Format(input);
        AssertIsValidParse(result);
        AssertNoChangeOnSecondFormat(result, formatter);
        return result;
    }

    private static void AssertIsValidParse(string result)
    {
        _ = new TSql160Parser(initialQuotedIdentifiers: false).ParseStatementList(new StringReader(result), out var errors);

        if (errors.Any())
        {
            Assert.Fail("Failed to parse the formatter output: "
                + string.Concat(errors.Select(e => Environment.NewLine + e.Message)) + Environment.NewLine
                + Environment.NewLine
                + "Formatter output:" + Environment.NewLine
                + result);
        }
    }

    private static void AssertNoChangeOnSecondFormat(string result, TSqlFormatter formatter)
    {
        var secondFormat = formatter.Format(result);
        if (secondFormat != result)
        {
            Assert.Fail("Running the formatter a second time caused further changes. Output of first pass: " + Environment.NewLine
                + Environment.NewLine
                + result + Environment.NewLine
                + Environment.NewLine
                + "Output of second pass:" + Environment.NewLine
                + Environment.NewLine
                + secondFormat);
        }
    }

    [Test]
    public static void INTO_clause_starts_on_new_line()
    {
        Format("select 1 into A").ShouldBe("""
            select 1
            into A
            """);
    }

    [Test]
    public static void FROM_clause_starts_on_new_line()
    {
        Format("select 1 from A").ShouldBe("""
            select 1
            from A
            """);
    }

    [Test]
    public static void FROM_clause_starts_on_same_line_for_select_asterisk_and_no_other_clauses()
    {
        Format("select * from A").ShouldBe("select * from A");
    }

    [Test]
    public static void WHERE_clause_starts_on_new_line()
    {
        Format("select 1 where A = 1").ShouldBe("""
            select 1
            where A = 1
            """);
    }

    [Test]
    public static void GROUP_BY_clause_starts_on_new_line()
    {
        Format("select 1 group by A").ShouldBe("""
            select 1
            group by A
            """);
    }

    [Test]
    public static void HAVING_clause_starts_on_new_line()
    {
        Format("select 1 group by A having A = 1").ShouldBe("""
            select 1
            group by A
            having A = 1
            """);
    }

    [Test]
    public static void WINDOW_clause_starts_on_new_line()
    {
        Format("select 1 from A window W as (partition by B)").ShouldBe("""
            select 1
            from A
            window W as (partition by B)
            """);
    }

    [Test]
    public static void WINDOW_clause_starts_on_new_line_when_window_name_is_window()
    {
        Format("select 1 from A window window as (partition by B)").ShouldBe("""
            select 1
            from A
            window window as (partition by B)
            """);
    }

    [Test]
    public static void ORDER_BY_clause_starts_on_new_line()
    {
        Format("select 1 order by A").ShouldBe("""
            select 1
            order by A
            """);
    }

    [Test]
    public static void OFFSET_clause_starts_on_new_line()
    {
        Format("select 1 order by A offset 10 rows").ShouldBe("""
            select 1
            order by A
            offset 10 rows
            """);
    }

    [TestCase("join")]
    [TestCase("inner join")]
    [TestCase("left join")]
    [TestCase("left outer join")]
    [TestCase("right join")]
    [TestCase("right outer join")]
    [TestCase("full join")]
    [TestCase("full outer join")]
    public static void Qualified_join_clause_starts_on_new_line(string joinType)
    {
        Format($"select 1 from A {joinType} B on A.Id = B.Id").ShouldBe($"""
            select 1
            from A
            {joinType} B on A.Id = B.Id
            """);
    }

    [TestCase("cross join")]
    [TestCase("cross apply")]
    [TestCase("outer apply")]
    public static void Unqualified_join_clause_starts_on_new_line(string joinType)
    {
        Format($"select 1 from A {joinType} B").ShouldBe($"""
            select 1
            from A
            {joinType} B
            """);
    }

    [TestCase("union")]
    [TestCase("union all")]
    [TestCase("intersect")]
    [TestCase("except")]
    public static void Binary_expression_operator_on_its_own_line(string operatorType)
    {
        Format($"select 1 {operatorType} select 1").ShouldBe($"""
            select 1
            {operatorType}
            select 1
            """);
    }

    [Test]
    public static void Nested_queries_are_indented()
    {
        Format("""
            select *
            from (
            select 1
            ) as q
            """).ShouldBe("""
            select *
            from (
                select 1
            ) as q
            """);
    }

    [Test]
    public static void Indentation_is_applied_to_added_newlines()
    {
        Format("select * from (select 1) as q").ShouldBe("""
            select * from (
                select 1
            ) as q
            """);
    }

    [Test]
    public static void Indentation_is_not_removed_when_already_present()
    {
        Format("""
            begin
                select 1
            end
            """).ShouldBe(
            """
            begin
                select 1
            end
            """);
    }

    [Test]
    public static void Indentation_is_not_added_when_already_present()
    {
        Format("""
            select * from (
                select
                    1
            ) as q
            """).ShouldBe(
            """
            select * from (
                select
                    1
            ) as q
            """);
    }

    [Test]
    public static void Indentation_is_applied_to_existing_newlines()
    {
        Format("""
            select * from (
            select
                1
            ) as q
            """).ShouldBe("""
            select * from (
                select
                    1
            ) as q
            """);
    }

    [Test]
    public static void Trailing_whitespace_is_not_added_to_empty_lines()
    {
        Format(string.Join(Environment.NewLine,
            "select * from (",
            "select",
            "", // <- No trailing whitespace
            "    1",
            ") as q")).SplitLines().ShouldBe([
                "select * from (",
                "    select",
                "", // <- No trailing whitespace should be added
                "        1",
                ") as q"]);
    }

    [Test]
    public static void Trailing_whitespace_is_removed()
    {
        Format(string.Join(Environment.NewLine,
            " ",
            "select ",
            " ",
            " ",
            "    1 ",
            " ")).SplitLines().ShouldBe([
                "",
                "select",
                "",
                "",
                "    1",
                ""]);
    }

    [Test]
    public static void Trailing_whitespace_is_not_removed_from_string_literals()
    {
        Format(string.Join(Environment.NewLine,
            "select ' ",
            " ",
            "'")).SplitLines().ShouldBe([
                "select ' ",
                " ",
                "'"]);
    }

    [Test]
    public static void Trailing_whitespace_is_not_removed_from_names()
    {
        Format(string.Join(Environment.NewLine,
            "select 1 as [ ",
            " ",
            "]")).SplitLines().ShouldBe([
            "select 1 as [ ",
            " ",
            "]"]);
    }

    [Test]
    public static void Multiple_spaces_are_normalized_when_opted_in()
    {
        Format("select  1", new() { NormalizeConsecutiveSpaces = true }).ShouldBe("select 1");
    }

    [Test]
    public static void Multiple_spaces_are_not_normalized_when_opted_out()
    {
        Format("select  1", new() { NormalizeConsecutiveSpaces = false }).ShouldBe("select  1");
    }

    [Test]
    public static void Keywords_are_recased([Values] KeywordCasing? casing)
    {
        Format("sELECT nULL", new() { KeywordCasing = casing }).ShouldBe(casing switch
        {
             KeywordCasing.Lowercase => "select null",
             KeywordCasing.Uppercase => "SELECT NULL",
             KeywordCasing.PascalCase => "Select Null",
             null => "sELECT nULL",
        });
    }

    [Test]
    public static void Built_in_functions_are_recased([Values] KeywordCasing? casing)
    {
        Format("select tRIM(' ')", new() { BuiltInFunctionCasing = casing }).ShouldBe(casing switch
        {
            KeywordCasing.Lowercase => "select trim(' ')",
            KeywordCasing.Uppercase => "select TRIM(' ')",
            KeywordCasing.PascalCase => "select Trim(' ')",
            null => "select tRIM(' ')",
        });
    }

    [Test]
    public static void AS_keyword_should_be_used_for_aliases_in_select_elements()
    {
        Format("select 1 A", new() { UseAsKeywordForAliases = true }).ShouldBe("select 1 as A");
    }

    [Test]
    public static void Added_AS_keyword_casing_is_sniffed([Values] KeywordCasing casing)
    {
        Format(casing switch
        {
            KeywordCasing.Lowercase => "select 1 A",
            KeywordCasing.Uppercase => "SELECT 1 A",
            KeywordCasing.PascalCase => "Select 1 A",
        }, new() { UseAsKeywordForAliases = true }).ShouldBe(casing switch
        {
            KeywordCasing.Lowercase => "select 1 as A",
            KeywordCasing.Uppercase => "SELECT 1 AS A",
            KeywordCasing.PascalCase => "Select 1 As A",
        });
    }

    [Test]
    public static void AS_keyword_should_be_used_for_aliases_in_table_references()
    {
        Format("select 1 from A B", new() { UseAsKeywordForAliases = true }).ShouldBe("""
            select 1
            from A as B
            """);
    }

    [Test]
    public static void AS_keyword_should_be_used_for_aliases_for_query_derived_tables()
    {
        Format("select 1 from (select 1) A", new() { UseAsKeywordForAliases = true }).ShouldBe("""
            select 1
            from (
                select 1
            ) as A
            """);
    }

    [Test]
    public static void AS_keyword_should_be_used_for_aliases_for_values()
    {
        Format("select 1 from (values (1)) A (B)", new() { UseAsKeywordForAliases = true }).ShouldBe("""
            select 1
            from (values (1)) as A (B)
            """);
    }

    [Test]
    public static void Identifiers_are_always_quoted_when_opted()
    {
        Format("select 1 as A, 2 as [B]", new() { IdentifierQuoting = IdentifierQuoting.Always }).ShouldBe("select 1 as [A], 2 as [B]");
    }

    [Test]
    public static void Identifiers_are_unquoted_when_opted()
    {
        Format("select 1 as A, 2 as [B]", new() { IdentifierQuoting = IdentifierQuoting.OnlyWhenNecessary }).ShouldBe("select 1 as A, 2 as B");
    }

    [Test]
    public static void Identifiers_are_left_as_is_when_quoting_is_necessary()
    {
        Format(@"select 1 as [Select], 2 as [A\B]", new() { IdentifierQuoting = IdentifierQuoting.OnlyWhenNecessary }).ShouldBe(@"select 1 as [Select], 2 as [A\B]");
    }

    [Test]
    public static void Identifiers_are_left_as_is_when_opted()
    {
        Format("select 1 as A, 2 as [B]", new() { IdentifierQuoting = null }).ShouldBe("select 1 as A, 2 as [B]");
    }

    [Test]
    public static void Identifier_quoting_does_not_affect_builtin_functions()
    {
        Format("select trim(' ')", new() { IdentifierQuoting = IdentifierQuoting.Always }).ShouldBe("select trim(' ')");
    }

    [Test]
    public static void Identifier_quoting_affects_user_defined_functions()
    {
        Format("select myfunc(' ')", new() { IdentifierQuoting = IdentifierQuoting.Always }).ShouldBe("select [myfunc](' ')");
    }

    [Test]
    public static void Multi_part_names_follow_opted_quoting([Values] IdentifierQuoting? identifierQuoting)
    {
        Format("select 1 from A.[B].C.[D]", new() { IdentifierQuoting = identifierQuoting }).ShouldBe(identifierQuoting switch
        {
            IdentifierQuoting.Always => """
                select 1
                from [A].[B].[C].[D]
                """,
            IdentifierQuoting.OnlyWhenNecessary => """
                select 1
                from A.B.C.D
                """,
            null => """
                select 1
                from A.[B].C.[D]
                """,
        });
    }

    [Test]
    public static void Semicolons_are_added_when_opted()
    {
        Format(
            """
            select 1
            if 1 = 1 select 2

            if 1 = 1
            begin
                select 3
            end

            begin try
                select 4
            end try
            begin catch
                select 5
            end catch
            """, new() { SemicolonUsage = SemicolonUsage.Always }).ShouldBe("""
                select 1;
                if 1 = 1 select 2;

                if 1 = 1
                begin;
                    select 3;
                end;

                begin try;
                    select 4;
                end try
                begin catch;
                    select 5;
                end catch;
                """);
    }


    [Test]
    public static void Semicolons_are_added_except_on_beginning_blocks_when_opted()
    {
        Format(
            """
            select 1
            if 1 = 1 select 2

            if 1 = 1
            begin
                select 3
            end

            begin try
                select 4
            end try
            begin catch
                select 5
            end catch
            """, new() { SemicolonUsage = SemicolonUsage.AlwaysExceptBeginningBlocks }).ShouldBe("""
                select 1;
                if 1 = 1 select 2;

                if 1 = 1
                begin
                    select 3;
                end;

                begin try
                    select 4;
                end try
                begin catch
                    select 5;
                end catch;
                """);
    }

    [Test]
    public static void Semicolons_are_removed_when_opted()
    {
        Format(
            """
            select 1;
            if 1 = 1 select 2;
            if 1 = 1
            begin;
                select 3;
            end;
            """, new() { SemicolonUsage = SemicolonUsage.OnlyWhenNecessary }).ShouldBe("""
                select 1
                if 1 = 1 select 2
                if 1 = 1
                begin
                    select 3
                end
                """);
    }

    [Test]
    public static void Semicolons_are_left_alone_when_opted()
    {
        Format(
            """
            select 1;
            if 1 = 1 select 2
            if 1 = 1
            begin
                select 3;
            end;
            """, new() { SemicolonUsage = null }).ShouldBe("""
                select 1;
                if 1 = 1 select 2
                if 1 = 1
                begin
                    select 3;
                end;
                """);
    }

    [Test]
    public static void Semicolons_are_not_added_when_disallowed()
    {
        Format(
            """
            while 0 = 1
                select 1;

            if 1 = 1
                select 1;
            else
                select 2;
            """, new() { SemicolonUsage = SemicolonUsage.Always }).ShouldBe("""
                while 0 = 1
                    select 1;

                if 1 = 1
                    select 1;
                else
                    select 2;
                """);
    }

    [Test]
    public static void Semicolons_are_not_removed_when_required()
    {
        Format(
            """
            select 1;
            with A as (select 1 as B) select * from A;
            """, new() { SemicolonUsage = SemicolonUsage.OnlyWhenNecessary }).ShouldBe("""
                select 1;
                with A as (select 1 as B) select * from A
                """);
    }

    [Test]
    public static void Semicolons_may_be_required_after_block_ends()
    {
        Format(
            """
            begin try;
                select 1;
            end try
            begin catch;
                select 1;
            end catch;
            with A as (select 1 as B) select * from A;

            begin;
                select 1;
            end;
            with A as (select 1 as B) select * from A;
            """, new() { SemicolonUsage = SemicolonUsage.OnlyWhenNecessary }).ShouldBe("""
                begin try
                    select 1
                end try
                begin catch
                    select 1
                end catch;
                with A as (select 1 as B) select * from A

                begin
                    select 1
                end;
                with A as (select 1 as B) select * from A
                """);
    }

    [Test]
    public static void Semicolons_are_not_required_after_block_begin()
    {
        Format(
            """
            begin try;
                with A as (select 1 as B) select * from A;
            end try
            begin catch;
                with A as (select 1 as B) select * from A;
            end catch;

            begin;
                with A as (select 1 as B) select * from A;
            end;
            """, new() { SemicolonUsage = SemicolonUsage.OnlyWhenNecessary }).ShouldBe("""
                begin try
                    with A as (select 1 as B) select * from A
                end try
                begin catch
                    with A as (select 1 as B) select * from A
                end catch

                begin
                    with A as (select 1 as B) select * from A
                end
                """);
    }
}
