namespace TSqlFormatter;

public static class Extensions
{
    /// <summary>
    /// Same functionality as <see cref="Array.FindLastIndex{T}(T[], Predicate{T})"/>.
    /// </summary>
    public static int FindLastIndex<T>(this ReadOnlySpan<T> span, Predicate<T> match)
    {
        for (var i = span.Length - 1; i >= 0; i--)
        {
            if (match(span[i]))
                return i;
        }

        return -1;
    }

    public static int BisectIndex<T>(this ReadOnlySpan<T> span, Func<T, bool> containsChange)
    {
        var min = 0;
        var max = span.Length - 1;

        while (min <= max)
        {
            var next = min + ((max - min) / 2);

            if (containsChange(span[next]))
            {
                if (next == min)
                    return next;

                max = next - 1;
            }
            else
            {
                min = next + 1;
            }
        }

        return min == span.Length ? -1 : min;
    }
}
