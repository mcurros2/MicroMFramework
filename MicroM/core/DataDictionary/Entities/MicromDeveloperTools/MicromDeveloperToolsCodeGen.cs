using MicroM.Configuration;
using MicroM.Core;
using MicroM.Data;
using MicroM.DataDictionary.CategoriesDefinitions;
using MicroM.Extensions;
using MicroM.Generators.ReactGenerator;
using MicroM.Generators.SQLGenerator;
using MicroM.Web.Authentication;
using MicroM.Web.Services;
using static System.ArgumentNullException;

namespace MicroM.DataDictionary.Entities;

public class MicromDeveloperToolsCodeGenDef : EntityDefinition
{
    public MicromDeveloperToolsCodeGenDef() : base("mcg", nameof(MicromDeveloperToolsCodeGen)) { Fake = true; }

    public string CategoriesFolder { get; set; } = "../Categories";

    public readonly Column<string> vc_classname = Column<string>.Text(column_flags: ColumnFlags.Get | ColumnFlags.PK);

    public readonly Column<string> vc_table = Column<string>.Text(size: 0);
    public readonly Column<string> vc_indexes = Column<string>.Text(size: 0);
    public readonly Column<string> vc_sp_get = Column<string>.Text(size: 0);
    public readonly Column<string> vc_sp_update = Column<string>.Text(size: 0);
    public readonly Column<string> vc_sp_iupdate = Column<string>.Text(size: 0);
    public readonly Column<string> vc_sp_updatei = Column<string>.Text(size: 0);
    public readonly Column<string> vc_sp_drop = Column<string>.Text(size: 0);
    public readonly Column<string> vc_sp_idrop = Column<string>.Text(size: 0);
    public readonly Column<string> vc_sp_dropi = Column<string>.Text(size: 0);
    public readonly Column<string> vc_sp_lookup = Column<string>.Text(size: 0);
    public readonly Column<string> vc_sp_brwStandard = Column<string>.Text(size: 0);
    public readonly Column<string> vc_custom_procs = Column<string>.Text(size: 0);

    public readonly Column<string> vc_react_definition = Column<string>.Text(size: 0);
    public readonly Column<string> vc_react_entity = Column<string>.Text(size: 0);
    public readonly Column<string> vc_react_categories = Column<string>.Text(size: 0);
    public readonly Column<string> vc_react_form = Column<string>.Text(size: 0);

}

public class MicromDeveloperToolsCodeGen : Entity<MicromDeveloperToolsCodeGenDef>
{
    public MicromDeveloperToolsCodeGen() : base() { }
    public MicromDeveloperToolsCodeGen(string? schema_name) : base(schema_name) { }
    public MicromDeveloperToolsCodeGen(IEntityClient ec, IMicroMEncryption? encryptor = null, string? schema_name = null) : base(ec, encryptor, schema_name) { }

    public override async Task<bool> GetData(CancellationToken ct, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IWebAPIServices? api = null, string? api_app_id = null)
    {
        ThrowIfNull(server_claims);

        string entity_name = Def.vc_classname.Value.ThrowIfNullOrEmpty(Def.vc_classname.Name);

        // MMC: this is the logged in user to the control panel
        server_claims.TryGetValue(MicroMServerClaimTypes.MicroMUserType_id, out var user_type);

        ThrowIfNull(user_type);
        ArgumentException.ThrowIfNullOrEmpty(api_app_id, nameof(api_app_id));

        ApplicationOption app_config = api?.app_config.GetAppConfiguration(api_app_id) ?? throw new InvalidOperationException($"App configuration not found for AppID {api_app_id}");

        if (!app_config.EnableDeveloperTools) throw new UnauthorizedAccessException("Developer tools are not enabled for this application");

        using IEntityClient client = Client.Clone();

        await client.Connect(ct);

        if (user_type as string != nameof(UserTypes.ADMIN)) throw new UnauthorizedAccessException("The logged in user does not have admin permissions");

        var schema_config = app_config.SchemaConfiguration;

        var entity_type = api.app_config.GetEntityType(api_app_id, entity_name) ?? throw new InvalidOperationException($"Entity not found. {api_app_id} {entity_name}");
        var ent = (EntityBase?)Activator.CreateInstance(entity_type) ?? throw new InvalidOperationException($"Can't create entity instance. {api_app_id} {entity_name}"); ;
        ent.Init(null, null, schema_config.APPSchema);


        var table = ent.AsCreateTable(schema_config, force: true);
        var idrop = ent.AsCreateDropProc(create_or_alter: true, with_idrop: true, force_fake: true);
        var drop = ent.AsCreateDropProc(create_or_alter: true, with_idrop: false, force_fake: true);

        var iupdate = ent.AsCreateUpdateProc(dd_schema: schema_config.DDSchema, create_or_alter: true, with_iupdate: true, force: true);
        var update = ent.AsCreateUpdateProc(dd_schema: schema_config.DDSchema, create_or_alter: true, with_iupdate: false, force: true);

        // MMC: get all categories types
        var assemblies = api.app_config.GetAllAPPAssemblies(api_app_id);
        var category_types = assemblies.GetAllCategoriesTypes();

        Def.vc_table.Value = table?.Count > 0 ? table[0] : "";
        Def.vc_indexes.Value = table?.Count > 1 ? table[1] : "";
        Def.vc_sp_update.Value = update?.Count > 0 ? update[0] : "";
        Def.vc_sp_iupdate.Value = iupdate?.Count > 0 ? iupdate[0] : "";
        Def.vc_sp_updatei.Value = iupdate?.Count > 1 ? iupdate[1] : "";
        Def.vc_sp_drop.Value = drop?.Count > 0 ? drop[0] : "";
        Def.vc_sp_idrop.Value = idrop?.Count > 0 ? idrop[0] : "";
        Def.vc_sp_dropi.Value = idrop?.Count > 1 ? idrop[1] : "";
        Def.vc_sp_get.Value = ent.AsCreateGetProc(true, force: true) ?? "";
        Def.vc_sp_lookup.Value = ent.AsCreateLookupProc(true, force: true) ?? "";
        Def.vc_sp_brwStandard.Value = ent.AsCreateViewProc(true, force: true) ?? "";
        Def.vc_custom_procs.Value = string.Join("\nGO\n\n", await ent.GetAllCustomProcs(ent.Def.Mneo, ct)) ?? "";

        Def.vc_react_entity.Value = ent.AsTypeScriptEntity();
        Def.vc_react_definition.Value = ent.AsTypeScriptEntityDefinition(app_config.TypeScriptCategoriesFolder);
        Def.vc_react_categories.Value = ent.Def.Columns.AsCategoriesEntities(category_types, app_config.TypeScriptDDCategoriesValuesClassName, app_config.TypeScriptDDCategoriesValuesClassImport, app_config.TypeScriptDDCategoryColumnName);
        Def.vc_react_form.Value = ent.AsTypeScriptEntityForm();

        return true;
    }
}


