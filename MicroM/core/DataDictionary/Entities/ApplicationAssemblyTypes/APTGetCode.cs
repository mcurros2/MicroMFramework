using MicroM.Configuration;
using MicroM.Core;
using MicroM.Data;
using MicroM.Extensions;
using MicroM.Generators.SQLGenerator;
using MicroM.Web.Authentication;
using MicroM.Web.Services;
using static MicroM.Database.DatabaseManagement;


namespace MicroM.DataDictionary
{
    public record APTGeneratedSQL
    {
        public string table { get; set; } = "";
        public string indexes { get; set; } = "";
        public string sp_get { get; set; } = "";
        public string sp_update { get; set; } = "";
        public string sp_iupdate { get; set; } = "";
        public string sp_updatei { get; set; } = "";
        public string sp_drop { get; set; } = "";
        public string sp_idrop { get; set; } = "";
        public string sp_dropi { get; set; } = "";
        public string sp_lookup { get; set; } = "";
        public string sp_brwStandard { get; set; } = "";
    }

    public record APTGeneratedReact
    {
        public string definition { get; set; } = "";
        public string entity { get; set; } = "";
        public string form { get; set; } = "";
    }

    public record APTGetCodeResult : EntityActionResult
    {
        public APTGeneratedSQL SQL = new APTGeneratedSQL();
        public APTGeneratedReact React = new APTGeneratedReact();
        public List<string> CustomSQL = new();
    }


    public class APTGetCode : EntityActionBase
    {
        public override async Task<EntityActionResult> Execute(EntityBase entity, DataWebAPIRequest parms, EntityDefinition def, MicroMOptions? Options, IMicroMWebAPI? API, IMicroMEncryption? encryptor, CancellationToken ct, string? api_app_id)
        {
            if (entity is not ApplicationAssemblyTypes) throw new InvalidOperationException($"This action can only be executed from {nameof(ApplicationAssemblyTypes)} class.");

            // Take note here that we are Defining the values names that should be sent from the client in the request
            // In this case
            string app_id = (string)parms.Values[nameof(app_id)];
            string assembly_id = (string)parms.Values[nameof(assembly_id)];
            string assemblytype_id = (string)parms.Values[nameof(assemblytype_id)];

            app_id.ThrowIfNullOrEmpty(nameof(app_id));
            assembly_id.ThrowIfNullOrEmpty(nameof(assembly_id));
            assemblytype_id.ThrowIfNullOrEmpty(nameof(assemblytype_id));

            var claims = parms.ServerClaims;
            if (claims == null) throw new ArgumentNullException(nameof(claims));

            // MMC: this is the logged in user to the control panel
            claims.TryGetValue(MicroMServerClaimTypes.MicroMUsername, out var admin_user_obj);
            claims.TryGetValue(MicroMServerClaimTypes.MicroMPassword, out var admin_password_obj);

            string? admin_user = (string?)admin_user_obj;
            string? admin_password = (string?)admin_password_obj;

            if (string.IsNullOrEmpty(admin_user)) throw new ArgumentNullException(nameof(claims));

            using IEntityClient admin_dbc = entity.Client.Clone(new_user: admin_user, new_password: admin_password ?? "");

            await admin_dbc.Connect(ct);

            if (!await LoggedInUserHasAdminRights(admin_dbc, ct)) throw new UnauthorizedAccessException("The logged in user does not have admin permissions");

            var eat = new EntitiesAssembliesTypes(admin_dbc);
            var entity_name = await eat.LookupData(ct) ?? throw new InvalidOperationException($"Can't find entity for AppID {app_id}, AssemblyID {assembly_id}, TypeID {assemblytype_id}");
            var entity_type = API?.GetEntityType(app_id, entity_name) ?? throw new InvalidOperationException($"Entity not found. {app_id} {entity_name}");
            var ent = (EntityBase?)Activator.CreateInstance(entity_type) ?? throw new InvalidOperationException($"Can't create entity instance. {app_id} {entity_name}"); ;

            var table = ent.AsCreateTable();
            var idrop = ent.AsCreateIDropProc(true);

            APTGetCodeResult result = new()
            {
                SQL = new()
                {
                    table = table[0],
                    indexes = table.Count > 1 ? table[1] : "",
                    sp_update = ent.GetUpdateProc(true),
                    sp_iupdate = ent.GetIUpdateProc(true),
                    sp_updatei = ent.GetUpdateForIUpdateProc(true),
                    sp_get = ent.AsCreateGetProc(true),
                    sp_lookup = ent.AsCreateLookupProc(true),
                    sp_brwStandard = ent.AsCreateViewProc(true),
                    sp_drop = ent.AsCreateNormalDropProc(true)[0],
                    sp_idrop = idrop[0],
                    sp_dropi = idrop[1]
                },
                CustomSQL = await ent.GetAllCustomProcs(ent.Def.Mneo, ct),
            };


            return result;
        }
    }
}
