using MicroM.Configuration;
using MicroM.Core;
using MicroM.Data;
using MicroM.Extensions;
using MicroM.Generators.ReactGenerator;
using MicroM.Generators.SQLGenerator;
using MicroM.Web.Authentication;
using MicroM.Web.Services;
using static MicroM.Database.DatabaseManagement;
using static System.ArgumentNullException;

namespace MicroM.DataDictionary;

public class EntityGeneratedCodeDef : EntityDefinition
{
    public EntityGeneratedCodeDef() : base("egc", nameof(EntityGeneratedCode)) { Fake = true; }

    public readonly Column<string> c_application_id = Column<string>.PK();
    public readonly Column<string> c_assembly_id = Column<string>.PK();
    public readonly Column<string> c_assemblytype_id = Column<string>.PK();

    public readonly Column<string> vc_table = new(sql_type: System.Data.SqlDbType.VarChar, size: 0);
    public readonly Column<string> vc_indexes = new(sql_type: System.Data.SqlDbType.VarChar, size: 0);
    public readonly Column<string> vc_sp_get = new(sql_type: System.Data.SqlDbType.VarChar, size: 0);
    public readonly Column<string> vc_sp_update = new(sql_type: System.Data.SqlDbType.VarChar, size: 0);
    public readonly Column<string> vc_sp_iupdate = new(sql_type: System.Data.SqlDbType.VarChar, size: 0);
    public readonly Column<string> vc_sp_updatei = new(sql_type: System.Data.SqlDbType.VarChar, size: 0);
    public readonly Column<string> vc_sp_drop = new(sql_type: System.Data.SqlDbType.VarChar, size: 0);
    public readonly Column<string> vc_sp_idrop = new(sql_type: System.Data.SqlDbType.VarChar, size: 0);
    public readonly Column<string> vc_sp_dropi = new(sql_type: System.Data.SqlDbType.VarChar, size: 0);
    public readonly Column<string> vc_sp_lookup = new(sql_type: System.Data.SqlDbType.VarChar, size: 0);
    public readonly Column<string> vc_sp_brwStandard = new(sql_type: System.Data.SqlDbType.VarChar, size: 0);
    public readonly Column<string> vc_custom_procs = new(sql_type: System.Data.SqlDbType.VarChar, size: 0);

    public readonly Column<string> vc_react_definition = new(sql_type: System.Data.SqlDbType.VarChar, size: 0);
    public readonly Column<string> vc_react_entity = new(sql_type: System.Data.SqlDbType.VarChar, size: 0);
    public readonly Column<string> vc_react_categories = new(sql_type: System.Data.SqlDbType.VarChar, size: 0);
    public readonly Column<string> vc_react_form = new(sql_type: System.Data.SqlDbType.VarChar, size: 0);
}

public class EntityGeneratedCode : Entity<EntityGeneratedCodeDef>
{
    public EntityGeneratedCode() : base() { }
    public EntityGeneratedCode(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

    public override async Task<bool> GetData(CancellationToken ct, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IWebAPIServices? api = null, string? api_app_id = null)
    {
        ThrowIfNull(server_claims);

        // MMC: this is the logged in user to the control panel
        server_claims.TryGetValue(MicroMServerClaimTypes.MicroMUsername, out var admin_user_obj);
        server_claims.TryGetValue(MicroMServerClaimTypes.MicroMPassword, out var admin_password_obj);

        string? admin_user = (string?)admin_user_obj;
        string? admin_password = (string?)admin_password_obj;

        ThrowIfNull(admin_user);
        ThrowIfNull(admin_password);


        string app_id = Def.c_application_id.Value.ThrowIfNullOrEmpty(Def.c_application_id.Name).Trim();
        string assembly_id = Def.c_assembly_id.Value.ThrowIfNullOrEmpty(Def.c_assembly_id.Name);
        string assembly_type_id = Def.c_assemblytype_id.Value.ThrowIfNullOrEmpty(Def.c_assemblytype_id.Name);


        using IEntityClient admin_dbc = Client.Clone(new_user: admin_user, new_password: admin_password ?? "");

        await admin_dbc.Connect(ct);

        if (!await LoggedInUserHasAdminRights(admin_dbc, ct)) throw new UnauthorizedAccessException("The logged in user does not have admin permissions");

        var eat = new EntitiesAssembliesTypes(admin_dbc);
        eat.Def.c_assembly_id.Value = assembly_id;
        eat.Def.c_assemblytype_id.Value = assembly_type_id;

        var entity_name = await eat.LookupData(ct) ?? throw new InvalidOperationException($"Can't find entity for AppID {app_id}, AssemblyID {assembly_id}, TypeID {assembly_type_id}");

        var entity_type = api?.app_config.GetEntityType(app_id, entity_name) ?? throw new InvalidOperationException($"Entity not found. {app_id} {entity_name}");
        var ent = (EntityBase?)Activator.CreateInstance(entity_type) ?? throw new InvalidOperationException($"Can't crete entity instance. {app_id} {entity_name}");

        var table = ent.AsCreateTable(true);
        var idrop = ent.AsCreateIDropProc(true, true);
        var drop = ent.AsCreateNormalDropProc(true, force_fake: true);

        // MMC: get all categories types
        var assemblies = api.app_config.GetAllAPPAssemblies(app_id);
        var category_types = assemblies.GetAllCategoriesTypes();

        Def.vc_table.Value = table?.Count > 0 ? table[0] : "";
        Def.vc_indexes.Value = table?.Count > 1 ? table[1] : "";
        Def.vc_sp_update.Value = ent.GetUpdateProc(true, true);
        Def.vc_sp_iupdate.Value = ent.GetIUpdateProc(true, true);
        Def.vc_sp_updatei.Value = ent.GetUpdateForIUpdateProc(true, true);
        Def.vc_sp_drop.Value = drop?.Count > 0 ? drop[0] : "";
        Def.vc_sp_idrop.Value = idrop?.Count > 0 ? idrop[0] : "";
        Def.vc_sp_dropi.Value = idrop?.Count > 1 ? idrop[1] : "";
        Def.vc_sp_get.Value = ent.AsCreateGetProc(true, force: true) ?? "";
        Def.vc_sp_lookup.Value = ent.AsCreateLookupProc(true, force: true) ?? "";
        Def.vc_sp_brwStandard.Value = ent.AsCreateViewProc(true, force: true) ?? "";
        Def.vc_custom_procs.Value = string.Join("\nGO\n\n", await ent.GetAllCustomProcs(ent.Def.Mneo, ct)) ?? "";

        Def.vc_react_entity.Value = ent.AsTypeScriptEntity();
        Def.vc_react_definition.Value = ent.AsTypeScriptEntityDefinition();
        Def.vc_react_categories.Value = ent.Def.Columns.AsCategoriesEntities(category_types);
        Def.vc_react_form.Value = ent.AsTypeScriptEntityForm();

        return true;

    }

}
