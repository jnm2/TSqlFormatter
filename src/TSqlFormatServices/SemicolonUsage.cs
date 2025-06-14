namespace TSqlFormatServices;

public enum SemicolonUsage
{
    Always,
    /// <summary>
    /// Same as <see cref="Always"/>, but does not place a semicolon after <c>BEGIN</c>, <c>BEGIN TRY</c>, or <c>BEGIN CATCH</c>.
    /// </summary>
    AlwaysExceptBeginningBlocks,
    OnlyWhenNecessary,
}