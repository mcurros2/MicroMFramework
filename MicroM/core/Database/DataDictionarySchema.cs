using MicroM.Configuration;
using MicroM.Core;
using MicroM.Data;
using MicroM.DataDictionary.CategoriesDefinitions;
using MicroM.DataDictionary.Configuration;
using MicroM.DataDictionary.Entities;
using MicroM.DataDictionary.StatusDefinitions;
using MicroM.Extensions;
using static MicroM.Database.DatabaseSchema;
using static MicroM.Database.DatabaseSchemaPermissions;

namespace MicroM.Database;

public static class DataDictionarySchema
{


    public static async Task AddToDataDictionary<T>(IEntityClient ec, CancellationToken ct, string? dd_schema_name = null) where T : EntityBase, new()
    {
        bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);
        try
        {
            T ent = new();
            ent.Init(ec);
            await ent.AddToDataDictionary(ct, dd_schema_name);
        }
        finally
        {
            if (should_close) await ec.Disconnect();
        }
    }

    public static async Task AddEntitiesToDataDictionary(IEntityClient ec, CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> options, CancellationToken ct, string? dd_schema_name = null)
    {
        bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);
        try
        {
            await ec.Connect(ct);
            foreach (var option in options.Values)
            {
                await option.EntityInstance.AddInstanceToDataDictionary(ct, dd_schema_name);
            }
        }
        finally
        {
            if (should_close) await ec.Disconnect();
        }
    }

    public static Dictionary<string, Type> GetCoreEntitiesTypes()
    {
        Dictionary<string, Type> result = [];

        result.TryAddType<Objects>();
        result.TryAddType<Numbering>();
        result.TryAddType<SystemProcs>();
        result.TryAddType<Categories>();
        result.TryAddType<CategoriesValues>();
        result.TryAddType<Status>();
        result.TryAddType<StatusValues>();
        result.TryAddType<ObjectsCategories>();
        result.TryAddType<ObjectsStatus>();
        result.TryAddType<MicromRoutes>();
        result.TryAddType<MicromUsers>();
        result.TryAddType<MicromUsersCat>();
        result.TryAddType<MicromUsersGroups>();
        result.TryAddType<MicromUsersGroupsMembers>();
        result.TryAddType<MicromUsersGroupsMenus>();
        result.TryAddType<MicromUsersDevices>();
        result.TryAddType<MicromUsersLoginHistory>();
        result.TryAddType<MicromMenus>();
        result.TryAddType<MicromMenusItems>();
        result.TryAddType<MicromMenusItemsAllowedRoutes>();
        result.TryAddType<FileStoreProcess>();
        result.TryAddType<FileStore>();
        result.TryAddType<FileStoreStatus>();
        result.TryAddType<EmailServiceConfiguration>();
        result.TryAddType<EmailServiceQueue>();
        result.TryAddType<EmailServiceQueueStatus>();
        result.TryAddType<EmailServiceTemplates>();
        result.TryAddType<ImportProcess>();
        result.TryAddType<ImportProcessErrors>();
        result.TryAddType<ImportProcessStatus>();
        result.TryAddType<ApplicationOidcActiveSessions>();

        return result;
    }


    public static CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> GetDataDictionaryEntitiesInstances(IEntityClient? ec = null, string? schema_name = null)
    {
        CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> result = new();

        result.TryAddEntities(create_or_alter: true, entities: [
            ec == null ? new Objects(schema_name) : new Objects(ec, schema_name: schema_name),
            ec == null ? new Numbering(schema_name) : new Numbering(ec, schema_name: schema_name),
            ec == null ? new SystemProcs(schema_name) : new SystemProcs(ec, schema_name: schema_name),
            ec == null ? new Categories(schema_name) : new Categories(ec, schema_name: schema_name),
            ec == null ? new CategoriesValues(schema_name) : new CategoriesValues(ec, schema_name: schema_name),
            ec == null ? new Status(schema_name) : new Status(ec, schema_name: schema_name),
            ec == null ? new StatusValues(schema_name) : new StatusValues(ec, schema_name: schema_name),
            ec == null ? new Classes(schema_name) : new Classes(ec, schema_name: schema_name),
            ec == null ? new ObjectsCategories(schema_name) : new ObjectsCategories(ec, schema_name: schema_name),
            ec == null ? new ObjectsStatus(schema_name) : new ObjectsStatus(ec, schema_name: schema_name),
            ec == null ? new FileStoreProcess(schema_name) : new FileStoreProcess(ec, schema_name: schema_name),
            ec == null ? new FileStore(schema_name) : new FileStore(ec, schema_name: schema_name),
            ec == null ? new FileStoreStatus(schema_name) : new FileStoreStatus(ec, schema_name: schema_name),
            ec == null ? new MicromRoutes(schema_name) : new MicromRoutes(ec, schema_name: schema_name),
            ec == null ? new MicromUsers(schema_name) : new MicromUsers(ec, schema_name: schema_name),
            ec == null ? new MicromUsersCat(schema_name) : new MicromUsersCat(ec, schema_name: schema_name),
            ec == null ? new MicromUsersLoginHistory(schema_name) : new MicromUsersLoginHistory(ec, schema_name: schema_name),
            ec == null ? new MicromUsersGroups(schema_name) : new MicromUsersGroups(ec, schema_name: schema_name),
            ec == null ? new MicromUsersDevices(schema_name) : new MicromUsersDevices(ec, schema_name: schema_name),
            ec == null ? new MicromUsersGroupsMembers(schema_name) : new MicromUsersGroupsMembers(ec, schema_name: schema_name),
            ec == null ? new MicromMenus(schema_name) : new MicromMenus(ec, schema_name: schema_name),
            ec == null ? new MicromMenusItems(schema_name) : new MicromMenusItems(ec, schema_name: schema_name),
            ec == null ? new MicromMenusItemsAllowedRoutes(schema_name) : new MicromMenusItemsAllowedRoutes(ec, schema_name: schema_name),
            ec == null ? new MicromUsersGroupsMenus(schema_name) : new MicromUsersGroupsMenus(ec, schema_name: schema_name),
            ec == null ? new EmailServiceConfiguration(schema_name) : new EmailServiceConfiguration(ec, schema_name: schema_name),
            ec == null ? new EmailServiceQueue(schema_name) : new EmailServiceQueue(ec, schema_name: schema_name),
            ec == null ? new EmailServiceQueueStatus(schema_name) : new EmailServiceQueueStatus(ec, schema_name: schema_name),
            ec == null ? new EmailServiceTemplates(schema_name) : new EmailServiceTemplates(ec, schema_name: schema_name),
            ec == null ? new ImportProcess(schema_name) : new ImportProcess(ec, schema_name: schema_name),
            ec == null ? new ImportProcessErrors(schema_name) : new ImportProcessErrors(ec, schema_name: schema_name),
            ec == null ? new ImportProcessStatus(schema_name) : new ImportProcessStatus(ec, schema_name: schema_name),
            ec == null ? new ApplicationOidcActiveSessions(schema_name) : new ApplicationOidcActiveSessions(ec, schema_name: schema_name),
            ]);

        return result;
    }

    public async static Task<CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>>?> CreateDatadictionarySchemaAndProcs(IEntityClient ec, AppDBSchemaConfiguration schema_config, CancellationToken ct, bool create_or_alter = false)
    {
        bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);

        CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>>? entities = null;
        try
        {
            await ec.Connect(ct);

            entities = GetDataDictionaryEntitiesInstances(ec, schema_config.DDSchema);
            var custom_procs_assembly = (entities[0]?.EntityType.Assembly) ?? throw new InvalidOperationException("Unable to determine the DataDictionary assembly for custom procedures.");
            var custom_procs = await custom_procs_assembly.GetAllClassifiedCustomSQLScripts(ct, schema_name: schema_config.DDSchema);

            var filtered_custom_procs = custom_procs.Filter(entities);

            await entities.CreateSchemaAndProcs(ec, schema_config, ct, create_or_alter, filtered_custom_procs);

            await CreateCategory<UserTypes>(ec, ct, schema_config.DDSchema);

            await CreateStatus<FileUpload>(ec, ct, schema_config.DDSchema);
            await CreateStatus<ProcessStatus>(ec, ct, schema_config.DDSchema);
            await CreateStatus<EmailStatus>(ec, ct, schema_config.DDSchema);
            await CreateStatus<ImportStatus>(ec, ct, schema_config.DDSchema);

            await entities.AddEntitiesToDataDictionary(ec, ct, schema_config.DDSchema);
        }
        finally
        {
            if (should_close) await ec.Disconnect();
        }

        return entities;
    }

    public async static Task GrantPermissionsToSystemProcs(IEntityClient ec, string login_or_group, AppDBSchemaConfiguration schema_config, CancellationToken ct)
    {
        CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> entities = GetDataDictionaryEntitiesInstances(ec, schema_name: schema_config.DDSchema);

        await GrantExecutionToAllProcs(ec, entities, login_or_group, ct);
    }


    /// <summary>
    /// Creates a <see cref="Categories"/> record and adds it to data dictionary tables
    /// </summary>
    public async static Task<Categories> CreateCategory<T>(IEntityClient ec, CancellationToken ct, string? schema_name = null) where T : CategoryDefinition, new()
    {
        T cst = new();
        return await cst.AddCategory(ec, ct, schema_name);
    }

    /// <summary>
    /// Creates a <see cref="Status"/> record and adds it to data dictionary tables
    /// </summary>
    public async static Task<Status> CreateStatus<T>(IEntityClient ec, CancellationToken ct, string? schema_name = null) where T : StatusDefinition, new()
    {
        T sst = new();
        return await sst.AddStatus(ec, ct, schema_name);
    }

    /// <summary>
    /// Creates the database schema for the specified entity type and adds it to the data dictionary asynchronously. 
    /// It will create the entity with the provided <see cref="AppDBSchemaConfiguration.APPSchema"/> 
    /// </summary>
    public static async Task<T> CreateSchemaAndDictionary<T>(
        IEntityClient ec,
        AppDBSchemaConfiguration schema_config,
        CancellationToken ct, bool create_or_alter = false, bool create_if_not_exists = true,
        bool create_custom_procs = false, bool drop_and_recreate_indexes = false, bool create_procs = true
        ) where T : EntityBase, new()
    {
        T ent = await CreateDBSchema<T>(ec, create_or_alter, create_if_not_exists, create_custom_procs, drop_and_recreate_indexes, create_procs, schema_config, ct);

        await ent.AddToDataDictionary(ct, schema_config.DDSchema);

        return ent;
    }


}
