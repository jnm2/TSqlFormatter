namespace TSqlFormatServices.Tests;

internal static class Extensions
{
    public static List<string> SplitLines(this string input)
    {
        var lines = new List<string>();

        var nextIndex = 0;

        while (input.IndexOf('\n', nextIndex) is (not -1) and var lfIndex)
        {
            var eolIndex = lfIndex > 0 && input[lfIndex - 1] == '\r'
                ? lfIndex - 1
                : lfIndex;

            lines.Add(input[nextIndex..eolIndex]);
            nextIndex = lfIndex + 1;
        }

        lines.Add(input[nextIndex..]);

        return lines;
    }
}
