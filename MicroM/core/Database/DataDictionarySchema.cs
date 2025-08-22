using MicroM.Core;
using MicroM.Data;
using MicroM.DataDictionary;
using MicroM.DataDictionary.CategoriesDefinitions;
using MicroM.DataDictionary.Configuration;
using MicroM.DataDictionary.StatusDefs;
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

    public static CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> GetDataDictionaryEntitiesTypes(IEntityClient ec)
    {
        CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> result = new();

        result.TryAddEntities(create_or_alter: true, entities: [
            new Objects(ec),
            new Numbering(ec),
            new SystemProcs(ec),
            new Categories(ec),
            new CategoriesValues(ec),
            new Status(ec),
            new StatusValues(ec),
            new Classes(ec),
            new ObjectsCategories(ec),
            new ObjectsStatus(ec),
            new FileStoreProcess(ec),
            new FileStore(ec),
            new FileStoreStatus(ec),
            new MicromRoutes(ec),
            new MicromUsers(ec),
            new MicromUsersCat(ec),
            new MicromUsersLoginHistory(ec),
            new MicromUsersGroups(ec),
            new MicromUsersDevices(ec),
            new MicromUsersGroupsMembers(ec),
            new MicromMenus(ec),
            new MicromMenusItems(ec),
            new MicromMenusItemsAllowedRoutes(ec),
            new MicromUsersGroupsMenus(ec),
            new EmailServiceConfiguration(ec),
            new EmailServiceQueue(ec),
            new EmailServiceQueueStatus(ec),
            new EmailServiceTemplates(ec),
            new ImportProcess(ec),
            new ImportProcessErrors(ec),
            new ImportProcessStatus(ec)
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
            custom_procs = await asm.GetAllClassifiedCustomProcs(ct);

            // Create types and sequences
            if (custom_procs?.Count > 0) await CreateAllCustomSQLTypes(ec, custom_procs, ct);

            entities = GetDataDictionaryEntitiesTypes(ec);

            // Tables and constraints
            created_tables = await CreateEntitiesInexistentTables(ec, entities, ct);
            await CreateEntitiesConstraintsAndIndexes(ec, created_tables, ct);

            // create custom tables is any
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

    public static Dictionary<string, Type> GetCoreEntitiesTypes()
    {
        Dictionary<string, Type> result = [];
        result.TryAddType<SystemProcs>();
        result.TryAddType<Categories>();
        result.TryAddType<CategoriesValues>();

        result.TryAddType<Status>();
        result.TryAddType<StatusValues>();

        result.TryAddType<MicromRoutes>();

        result.TryAddType<MicromUsersLoginHistory>();
        result.TryAddType<MicromUsersGroups>();
        result.TryAddType<MicromUsers>();
        result.TryAddType<MicromUsersCat>();
        result.TryAddType<MicromUsersDevices>();
        result.TryAddType<MicromUsersGroupsMembers>();

        result.TryAddType<MicromMenus>();
        result.TryAddType<MicromMenusItems>();
        result.TryAddType<MicromMenusItemsAllowedRoutes>();

        result.TryAddType<MicromUsersGroupsMenus>();

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

        return result;
    }

    public async static Task GrantPermissionsToSystemProcs(IEntityClient ec, string login_or_group, CancellationToken ct)
    {
        CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> entities = GetDataDictionaryEntitiesTypes(ec);

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
        bool create_custom_procs = false, bool drop_and_recreate_indexes = false, bool create_procs = true
        ) where T : EntityBase, new()
    {
        T ent = await CreateSchema<T>(ec, create_or_alter, create_if_not_exists, create_custom_procs, drop_and_recreate_indexes, create_procs, ct);

        await ent.AddToDataDictionary(ct);

        return ent;
    }


}
