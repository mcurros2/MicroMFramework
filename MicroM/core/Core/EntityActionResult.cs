namespace MicroM.Core
{
    /// <summary>
    /// Represents an empty action result.
    /// </summary>
    public record EmptyActionResult : EntityActionResult
    {
    }

    /// <summary>
    /// Base record for action results returned by entity actions.
    /// </summary>
    public abstract record EntityActionResult
    {
    }
}
