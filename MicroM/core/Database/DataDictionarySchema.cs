using MicroM.Core;
using MicroM.Data;
using MicroM.DataDictionary.CategoriesDefinitions;
using MicroM.DataDictionary.Configuration;
using MicroM.DataDictionary.Entities;
using MicroM.DataDictionary.StatusDefinitions;
using MicroM.Extensions;
using System.Reflection;
using static MicroM.Database.DatabaseSchema;
using static MicroM.Database.DatabaseSchemaPermissions;
using static MicroM.Database.DatabaseSchemaTables;

namespace MicroM.Database;

public static class DataDictionarySchema
{


    public static async Task AddToDataDictionary<T>(IEntityClient ec, CancellationToken ct) where T : EntityBase, new()
    {
        bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);
        try
        {
            T ent = new();
            ent.Init(ec);
            await ent.AddToDataDictionary(ct);
        }
        finally
        {
            if (should_close) await ec.Disconnect();
        }
    }

    public static async Task AddEntitiesToDataDictionary(IEntityClient ec, CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> options, CancellationToken ct)
    {
        bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);
        try
        {
            await ec.Connect(ct);
            foreach (var option in options.Values)
            {
                await option.EntityInstance.AddInstanceToDataDictionary(ct);
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


    public static CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> GetDataDictionaryEntitiesInstances(IEntityClient? ec = null)
    {
        CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> result = new();

        result.TryAddEntities(create_or_alter: true, entities: [
            ec == null ? new Objects() : new Objects(ec),
            ec == null ? new Numbering() : new Numbering(ec),
            ec == null ? new SystemProcs() : new SystemProcs(ec),
            ec == null ? new Categories() : new Categories(ec),
            ec == null ? new CategoriesValues() : new CategoriesValues(ec),
            ec == null ? new Status() : new Status(ec),
            ec == null ? new StatusValues() : new StatusValues(ec),
            ec == null ? new Classes() : new Classes(ec),
            ec == null ? new ObjectsCategories() : new ObjectsCategories(ec),
            ec == null ? new ObjectsStatus() : new ObjectsStatus(ec),
            ec == null ? new FileStoreProcess() : new FileStoreProcess(ec),
            ec == null ? new FileStore() : new FileStore(ec),
            ec == null ? new FileStoreStatus() : new FileStoreStatus(ec),
            ec == null ? new MicromRoutes() : new MicromRoutes(ec),
            ec == null ? new MicromUsers() : new MicromUsers(ec),
            ec == null ? new MicromUsersCat() : new MicromUsersCat(ec),
            ec == null ? new MicromUsersLoginHistory() : new MicromUsersLoginHistory(ec),
            ec == null ? new MicromUsersGroups() : new MicromUsersGroups(ec),
            ec == null ? new MicromUsersDevices() : new MicromUsersDevices(ec),
            ec == null ? new MicromUsersGroupsMembers() : new MicromUsersGroupsMembers(ec),
            ec == null ? new MicromMenus() : new MicromMenus(ec),
            ec == null ? new MicromMenusItems() : new MicromMenusItems(ec),
            ec == null ? new MicromMenusItemsAllowedRoutes() : new MicromMenusItemsAllowedRoutes(ec),
            ec == null ? new MicromUsersGroupsMenus() : new MicromUsersGroupsMenus(ec),
            ec == null ? new EmailServiceConfiguration() : new EmailServiceConfiguration(ec),
            ec == null ? new EmailServiceQueue() : new EmailServiceQueue(ec),
            ec == null ? new EmailServiceQueueStatus() : new EmailServiceQueueStatus(ec),
            ec == null ? new EmailServiceTemplates() : new EmailServiceTemplates(ec),
            ec == null ? new ImportProcess() : new ImportProcess(ec),
            ec == null ? new ImportProcessErrors() : new ImportProcessErrors(ec),
            ec == null ? new ImportProcessStatus() : new ImportProcessStatus(ec),
            ec == null ? new ApplicationOidcActiveSessions() : new ApplicationOidcActiveSessions(ec),
            ]);

        return result;
    }

    public async static Task CreateDatadictionarySchemaAndProcs(IEntityClient ec, CancellationToken ct, bool create_or_alter = false)
    {
        bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);

        CustomOrderedDictionary<CustomScript>? custom_procs = null;
        CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>>? entities = null;
        CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>>? created_tables = null;
        try
        {
            await ec.Connect(ct);

            Assembly asm = typeof(Objects).Assembly;
            custom_procs = await asm.GetAllClassifiedCustomProcs(ct, replace_dd_schema: true);

            entities = GetDataDictionaryEntitiesInstances(ec);

            // Tables and constraints
            await CreateAllInexistingSchemas(ec, entities, ct);

            // Create types and sequences
            if (custom_procs?.Count > 0) await CreateAllCustomSQLTypes(ec, custom_procs, ct);

            created_tables = await CreateEntitiesInexistentTables(ec, entities, ct);
            await CreateEntitiesConstraintsAndIndexes(ec, created_tables, ct);

            // create custom tables if any
            if (custom_procs?.Count > 0)
            {
                await CreateAllCustomTables(ec, custom_procs, ct);
                await CreateAllCustomViews(ec, custom_procs, ct);
            }

            await CreateAllEntitiesProcs(ec, entities, custom_procs, ct, create_or_alter);

            await CreateCategory<UserTypes>(ec, ct);
            await CreateCategory<IdentityProviderRole>(ec, ct);

            await CreateStatus<FileUpload>(ec, ct);
            await CreateStatus<ProcessStatus>(ec, ct);
            await CreateStatus<EmailStatus>(ec, ct);
            await CreateStatus<ImportStatus>(ec, ct);

            // MMC: add to data dictionary
            await AddEntitiesToDataDictionary(ec, entities, ct);

        }
        finally
        {
            if (custom_procs?.Count > 0) custom_procs.Clear();
            if (entities?.Count > 0) entities.Clear();
            if (created_tables?.Count > 0) created_tables.Clear();
            if (should_close) await ec.Disconnect();
        }
    }

    public async static Task GrantPermissionsToSystemProcs(IEntityClient ec, string login_or_group, CancellationToken ct)
    {
        CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> entities = GetDataDictionaryEntitiesInstances(ec);

        await GrantExecutionToAllProcs(ec, entities, login_or_group, ct);
    }


    /// <summary>
    /// Creates a <see cref="Categories"/> record and adds it to data dictionary tables
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="ec"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async static Task<Categories> CreateCategory<T>(IEntityClient ec, CancellationToken ct) where T : CategoryDefinition, new()
    {
        T cst = new();
        return await cst.AddCategory(ec, ct);
    }

    /// <summary>
    /// Creates a <see cref="Status"/> record and adds it to data dictionary tables
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="ec"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async static Task<Status> CreateStatus<T>(IEntityClient ec, CancellationToken ct) where T : StatusDefinition, new()
    {
        T sst = new();
        return await sst.AddStatus(ec, ct);
    }

    public static async Task<T> CreateSchemaAndDictionary<T>(
        IEntityClient ec, CancellationToken ct, bool create_or_alter = false, bool create_if_not_exists = true,
        bool create_custom_procs = false, bool drop_and_recreate_indexes = false, bool create_procs = true,
        bool replace_dd_schema = false
        ) where T : EntityBase, new()
    {
        T ent = await CreateDBSchema<T>(ec, create_or_alter, create_if_not_exists, create_custom_procs, drop_and_recreate_indexes, create_procs, ct, replace_dd_schema);

        await ent.AddToDataDictionary(ct);

        return ent;
    }


}
