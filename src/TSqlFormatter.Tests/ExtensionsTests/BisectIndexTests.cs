namespace TSqlFormatter.Tests.ExtensionsTests;

public static class BisectIndexTests
{
    public static IEnumerable<TestCaseData> AssertStepsCases()
    {
        for (var length = 0; length <= 10; length++)
        {
            for (var changeIndex = -1; changeIndex < length; changeIndex++)
            {
                yield return new TestCaseData(length, changeIndex);
            }
        }
    }

    [TestCaseSource(nameof(AssertStepsCases))]
    public static void AssertSteps(int length, int changeIndex)
    {
        var indexesAsElements = Enumerable.Range(0, length).ToArray();

        var testedIndexes = new List<int>();

        var result = indexesAsElements.BisectIndex(containsChange: element =>
        {
            testedIndexes.Add(element);
            return changeIndex != -1 && element >= changeIndex;
        });

        result.ShouldBe(changeIndex, "Tested indexes: " + string.Join(", ", testedIndexes));
    }
}
