using MicroM.Data;

namespace MicroM.Core
{
    public record EntityActionArguments
    {
        public required DataWebAPIRequest WebParms { get; init; }
    }
}
