using MicroM.Configuration;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.Core
{
    /// <summary>
    /// Base class for entity actions.
    /// </summary>
    public abstract class EntityActionBase
    {
        /// <summary>
        /// Executes the action.
        /// </summary>
        /// <param name="entity">The entity the action applies to.</param>
        /// <param name="parms">Web request parameters.</param>
        /// <param name="def">Entity definition.</param>
        /// <param name="Options">Optional configuration.</param>
        /// <param name="API">Optional web API services.</param>
        /// <param name="encryptor">Optional encryption provider.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <param name="app_id">Optional application identifier.</param>
        /// <returns>The action result.</returns>
        public abstract Task<EntityActionResult> Execute(EntityBase entity, DataWebAPIRequest parms, EntityDefinition def, MicroMOptions? Options, IWebAPIServices? API, IMicroMEncryption? encryptor, CancellationToken ct, string? app_id);
    }
}
