using MicroM.Configuration;
using MicroM.Core;
using MicroM.Data;
using MicroM.DataDictionary.Entities.MicromUsers;
using MicroM.ImportData;
using MicroM.Web.Authentication;
using MicroM.Web.Services.Security;
using System.Reflection;

namespace MicroM.Web.Services
{
    public interface IMicroMWebAPI
    {

        /// <summary>
        /// Creates an Entity if exists in the configured assembly <see cref="LoadEntityTypes(Assembly)"/>.
        /// </summary>
        /// <param name="entity_name"></param>
        /// <param name="ec"></param>
        /// <returns></returns>
        public EntityBase? CreateEntity(ApplicationOption app, string entity_name, Dictionary<string, object>? server_claims, IAuthenticationProvider auth, IEntityClient? ec = null);
        public EntityBase? CreateEntity(string app_id, string entity_name, Dictionary<string, object>? server_claims, IAuthenticationProvider auth, CancellationToken ct);

        /// <summary>
        /// Connection factory for the webAPI.
        /// </summary>
        /// <returns></returns>
        public IEntityClient CreateDbConnection(ApplicationOption app, Dictionary<string, object>? server_claims, IAuthenticationProvider auth);

        public Task<IEntityClient> CreateDbConnection(string app_id, Dictionary<string, object>? server_claims, IAuthenticationProvider auth, CancellationToken ct);

        // TODO: dynamic entities
        public EntityDefinition? HandleGetEntityDefinition(string app_id, string entity_name);

        // Login
        public Task<(LoginResult? user_data, TokenResult? token_result)> HandleLogin(IAuthenticationProvider auth, WebAPIJsonWebTokenHandler jwt_handler, string app_id, UserLogin user_login, Dictionary<string, object> server_claims, CancellationToken ct);

        public Task<(RefreshTokenResult? result, TokenResult? token_result)> HandleRefreshToken(IAuthenticationProvider auth, WebAPIJsonWebTokenHandler jwt_handler, string app_id, UserRefreshTokenRequest refreshRequest, CancellationToken ct);

        public Task HandleLogoff(IAuthenticationProvider auth, string app_id, string user_name, CancellationToken ct);

        public Task<(bool failed, string? error_message)> HandleSendRecoveryEmail(IAuthenticationProvider auth, string app_id, string user_name, CancellationToken ct);

        public Task<(bool failed, string? error_message)> HandleRecoverPassword(IAuthenticationProvider auth, string app_id, string user_name, string new_password, string recovery_code, CancellationToken ct);

        // Entities
        public Task<Dictionary<string, object?>?> HandleGetEntity(IAuthenticationProvider auth, string app_id, string entity_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct);

        public Task<DBStatusResult?> HandleUpdateEntity(IAuthenticationProvider auth, string app_id, string entity_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct);

        public Task<DBStatusResult?> HandleDeleteEntity(IAuthenticationProvider auth, string app_id, string entity_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct);

        public Task<DBStatusResult?> HandleInsertEntity(IAuthenticationProvider auth, string app_id, string entity_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct);

        public Task<LookupResult> HandleLookupEntity(IAuthenticationProvider auth, string app_id, string entity_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct, string? lookup_name = null);

        public Task<List<DataResult>?> HandleExecuteView(IAuthenticationProvider auth, string app_id, string entity_name, string view_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct);

        public Task<List<DataResult>?> HandleExecuteProc(IAuthenticationProvider auth, string app_id, string entity_name, string proc_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct);

        public Task<DBStatusResult?> HandleExecuteProcDBStatus(IAuthenticationProvider auth, string app_id, string entity_name, string proc_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct);

        public Task<EntityActionResult?> HandleExecuteAction(IAuthenticationProvider auth, string app_id, string entity_name, string entity_action, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct);

        public Task<CSVImportResult?> HandleImportData(IAuthenticationProvider auth, string app_id, string entity_name, string? import_proc, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct);

        // Configuration
        public Task<bool> RefreshConfig(string? app_id, CancellationToken ct);

        public Type? GetEntityType(string app_id, string entity_name);

        public List<Assembly> GetAllAPPAssemblies(string app_id);

        // File upload and serve
        public Task<UploadFileResult> HandleUpload(string app_id, string fileprocess_id, string file_name, Stream file_data, int? maxSize, int? quality, IEntityClient ec, CancellationToken ct);

        public Task<ServeFileResult?> HandleServe(string app_id, string fileguid, IEntityClient ec, CancellationToken ct);

        public Task<ServeFileResult?> HandleServeThumbnail(string app_id, string fileguid, int? maxSize, int? quality, IEntityClient ec, CancellationToken ct);

        public Dictionary<string, object> GetApplicationKeys(string app_id);

        // Backgroudn tasks
        public IBackgroundTaskQueue Queue { get; }

        // Emails
        public IEmailService EmailService { get; }

        // Security
        public ISecurityService SecurityService { get; }


    }



}
