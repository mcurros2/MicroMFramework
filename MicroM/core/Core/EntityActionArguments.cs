using MicroM.Data;

namespace MicroM.Core
{
    /// <summary>
    /// Encapsulates arguments passed to an entity action.
    /// </summary>
    public record EntityActionArguments
    {
        /// <summary>
        /// Gets the web request parameters for the action.
        /// </summary>
        public required DataWebAPIRequest WebParms { get; init; }
    }
}
