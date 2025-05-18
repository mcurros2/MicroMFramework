using MicroM.Core;
using MicroM.Data;
using MicroM.DataDictionary;
using MicroM.DataDictionary.CategoriesDefinitions;
using MicroM.DataDictionary.Configuration;
using MicroM.DataDictionary.StatusDefs;
using MicroM.Extensions;
using MicroM.Generators.SQLGenerator;
using static MicroM.Database.DatabaseManagement;

namespace MicroM.Configuration
{
    public static class DatabaseSchema
    {

        public static async Task AddToDataDictionary<T>(IEntityClient ec, CancellationToken ct) where T : EntityBase, new()
        {
            T ent = new();
            ent.Init(ec);
            await ent.AddToDataDictionary(ct);

        }

        public static async Task<bool> TableExists(IEntityClient ec, string table_name, string schema_name, CancellationToken ct)
        {
            string query = $"SELECT count(*) FROM information_schema.tables WHERE table_schema = '{schema_name}' AND table_name = '{table_name}'";
            return await ec.ExecuteSQLSingleColumn<int>(query, ct) == 1;
        }

        public static async Task CreateCustomProcs<T>(T? ent, IEntityClient ec, CancellationToken ct) where T : EntityBase, new()
        {
            T new_ent;

            if (ent == null)
            {
                new_ent = new();
                new_ent.Init(ec);
            }
            else
            {
                new_ent = ent;
            }

            foreach (string script in await new_ent.GetAllCustomProcs(new_ent.Def.Mneo, ct))
            {
                await ec.ExecuteSQLNonQuery(script, ct);
            }
        }

        public static async Task<T> CreateSchema<T>(
            IEntityClient ec, bool create_or_alter, bool with_iupdate, bool create_if_not_exists, bool with_idrop, bool create_custom_procs,
            CancellationToken ct
            ) where T : EntityBase, new()
        {
            T ent = new();
            ent.Init(ec);

            if (ent.Def.Fake == false)
            {
                bool create = true;
                bool table_exists = await TableExists(ec, ent.Def.TableName, "dbo", ct);

                if (create_if_not_exists)
                {
                    create = !table_exists;
                }

                if (create)
                {
                    await ec.ExecuteSQLNonQuery(ent.AsCreateTable(table_and_primary_key_only: true), ct);
                }

                if (table_exists || create)
                {
                    // Drop and recreate foreign keys, uniques, indexes
                    if (table_exists)
                    {
                        await ec.ExecuteSQLNonQuery(ent.AsDropIndexes() ?? "", ct);
                        await ec.ExecuteSQLNonQuery(ent.AsDropForeignKeys() ?? "", ct);
                        await ec.ExecuteSQLNonQuery(ent.AsDropUniqueConstraints() ?? "", ct);
                    }

                    await ec.ExecuteSQLNonQuery(ent.AsAlterPrimaryKey() ?? "", ct);
                    await ec.ExecuteSQLNonQuery(ent.AsAlterUniqueConstraints() ?? "", ct);
                    await ec.ExecuteSQLNonQuery(ent.AsAlterForeignKeys(with_drop: false) ?? "", ct);
                    await ec.ExecuteSQLNonQuery(ent.AsAlterIndexes() ?? "", ct);

                    await ec.ExecuteSQLNonQuery(ent.AsCreateUpdateProc(create_or_alter, with_iupdate), ct);
                    await ec.ExecuteSQLNonQuery(ent.AsCreateGetProc(create_or_alter), ct);
                    await ec.ExecuteSQLNonQuery(ent.AsCreateDropProc(create_or_alter, with_idrop), ct);
                    await ec.ExecuteSQLNonQuery(ent.AsCreateLookupProc(create_or_alter), ct);
                    await ec.ExecuteSQLNonQuery(ent.AsCreateViewProc(create_or_alter), ct);
                    if (create_custom_procs) await CreateCustomProcs<T>(ent, ec, ct);
                }

            }
            else
            {
                if (create_custom_procs) await CreateCustomProcs<T>(ent, ec, ct);
            }

            return ent;
        }

        public static async Task<T> CreateSchemaAndDictionary<T>(IEntityClient ec, CancellationToken ct, bool create_or_alter = false, bool with_iupdate = false, bool create_if_not_exists = true, bool with_idrop = false, bool create_custom_procs = false) where T : EntityBase, new()
        {
            T ent = await CreateSchema<T>(ec, create_or_alter, with_iupdate, create_if_not_exists, with_idrop, create_custom_procs, ct);

            await ent.AddToDataDictionary(ct);

            return ent;
        }

        public async static Task CreateConfigurationDBSchemaAndProcs(IEntityClient ec, CancellationToken ct, bool create_or_alter = false)
        {
            bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);
            try
            {
                await ec.Connect(ct);
                await CreateDatadictionarySchemaAndProcs(ec, ct, create_or_alter);
                await CreateCategory<AuthenticationTypes>(ec, ct);
                await CreateSchemaAndDictionary<EntitiesAssemblies>(ec, ct, with_iupdate: true, with_idrop: true, create_custom_procs: true, create_or_alter: create_or_alter);
                await CreateSchemaAndDictionary<EntitiesAssembliesTypes>(ec, ct, with_iupdate: true, with_idrop: true, create_custom_procs: true, create_or_alter: create_or_alter);
                await CreateSchemaAndDictionary<Applications>(ec, ct, with_iupdate: true, create_custom_procs: true, create_or_alter: create_or_alter);
                await CreateSchemaAndDictionary<ApplicationsCat>(ec, ct, create_custom_procs: true, create_or_alter: create_or_alter);
                await CreateSchemaAndDictionary<ApplicationsAssemblies>(ec, ct, with_iupdate: true, with_idrop: true, create_custom_procs: true, create_or_alter: create_or_alter);
                await CreateSchemaAndDictionary<ApplicationAssemblyTypes>(ec, ct, create_custom_procs: true, create_or_alter: create_or_alter);
                await CreateSchemaAndDictionary<ApplicationsUrls>(ec, ct, with_iupdate: true, create_custom_procs: true, create_or_alter: create_or_alter);
            }
            finally
            {
                if (should_close) await ec.Disconnect();
            }
        }


        public async static Task CreateDatadictionarySchemaAndProcs(IEntityClient ec, CancellationToken ct, bool create_or_alter = false)
        {
            bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);
            try
            {
                await ec.Connect(ct);

                // MMC: Order is important here
                await CreateSchema<SystemProcs>(ec, create_or_alter, false, true, false, true, ct);
                await CreateSchema<Objects>(ec, create_or_alter, false, true, false, true, ct);
                await CreateSchema<Numbering>(ec, create_or_alter, false, true, false, true, ct);

                await CreateSchemaAndDictionary<Classes>(ec, ct, create_or_alter, create_custom_procs: true);
                await CreateSchemaAndDictionary<Categories>(ec, ct, create_or_alter, create_custom_procs: true);
                await CreateSchemaAndDictionary<CategoriesValues>(ec, ct, create_or_alter, create_custom_procs: true);
                await CreateSchemaAndDictionary<Status>(ec, ct, create_or_alter, create_custom_procs: true);
                await CreateSchemaAndDictionary<StatusValues>(ec, ct, create_or_alter, create_custom_procs: true);
                await CreateSchemaAndDictionary<ObjectsCategories>(ec, ct, create_or_alter, create_custom_procs: true);
                await CreateSchemaAndDictionary<ObjectsStatus>(ec, ct, create_or_alter, create_custom_procs: true);

                await CreateCategory<UserTypes>(ec, ct);
                await CreateStatus<FileUpload>(ec, ct);
                await CreateStatus<ProcessStatus>(ec, ct);
                await CreateStatus<EmailStatus>(ec, ct);
                await CreateStatus<ImportStatus>(ec, ct);

                await CreateSchemaAndDictionary<FileStoreProcess>(ec, ct, create_or_alter, with_iupdate: true, with_idrop: true);
                await CreateSchemaAndDictionary<FileStore>(ec, ct, create_or_alter, with_iupdate: true, with_idrop: true);
                await CreateSchemaAndDictionary<FileStoreStatus>(ec, ct, create_or_alter, with_iupdate: true);

                await CreateCustomProcs<FileStoreProcess>(null, ec, ct);
                await CreateCustomProcs<FileStore>(null, ec, ct);
                await CreateCustomProcs<FileStoreStatus>(null, ec, ct);

                await CreateSchemaAndDictionary<MicromRoutes>(ec, ct, create_or_alter: create_or_alter);
                await CreateSchemaAndDictionary<MicromUsersLoginHistory>(ec, ct, create_or_alter: create_or_alter, with_iupdate: true);

                await CreateSchemaAndDictionary<MicromUsersGroups>(ec, ct, create_or_alter: create_or_alter);
                await CreateSchemaAndDictionary<MicromUsers>(ec, ct, create_or_alter, with_iupdate: true, with_idrop: true);
                await CreateSchemaAndDictionary<MicromUsersDevices>(ec, ct, create_or_alter, with_iupdate: true);
                await CreateSchemaAndDictionary<MicromUsersCat>(ec, ct, create_or_alter: create_or_alter);
                await CreateSchemaAndDictionary<MicromUsersGroupsMembers>(ec, ct, create_or_alter: create_or_alter);

                await CreateSchemaAndDictionary<MicromMenus>(ec, ct, create_or_alter: create_or_alter);
                await CreateSchemaAndDictionary<MicromMenusItems>(ec, ct, create_or_alter: create_or_alter);
                await CreateSchemaAndDictionary<MicromMenusItemsAllowedRoutes>(ec, ct, create_or_alter: create_or_alter, with_iupdate: true);

                await CreateSchemaAndDictionary<MicromUsersGroupsMenus>(ec, ct, create_or_alter: create_or_alter);

                await CreateCustomProcs<MicromUsersLoginHistory>(null, ec, ct);
                await CreateCustomProcs<MicromRoutes>(null, ec, ct);
                await CreateCustomProcs<MicromUsers>(null, ec, ct);
                await CreateCustomProcs<MicromUsersDevices>(null, ec, ct);
                await CreateCustomProcs<MicromUsersCat>(null, ec, ct);
                await CreateCustomProcs<MicromMenus>(null, ec, ct);
                await CreateCustomProcs<MicromMenusItems>(null, ec, ct);
                await CreateCustomProcs<MicromMenusItemsAllowedRoutes>(null, ec, ct);
                await CreateCustomProcs<MicromUsersGroupsMenus>(null, ec, ct);
                await CreateCustomProcs<MicromUsersGroups>(null, ec, ct);
                await CreateCustomProcs<MicromUsersGroupsMembers>(null, ec, ct);

                await CreateSchemaAndDictionary<EmailServiceConfiguration>(ec, ct, create_or_alter: create_or_alter, with_iupdate: true, with_idrop: true);
                await CreateSchemaAndDictionary<EmailServiceQueue>(ec, ct, create_or_alter: create_or_alter);
                await CreateSchemaAndDictionary<EmailServiceQueueStatus>(ec, ct, create_or_alter: create_or_alter);
                await CreateSchemaAndDictionary<EmailServiceTemplates>(ec, ct, create_or_alter: create_or_alter, with_iupdate: true, with_idrop: true);

                await CreateCustomProcs<EmailServiceConfiguration>(null, ec, ct);
                await CreateCustomProcs<EmailServiceQueue>(null, ec, ct);
                await CreateCustomProcs<EmailServiceQueueStatus>(null, ec, ct);
                await CreateCustomProcs<EmailServiceTemplates>(null, ec, ct);

                await CreateSchemaAndDictionary<ImportProcess>(ec, ct, create_or_alter: create_or_alter);
                await CreateSchemaAndDictionary<ImportProcessErrors>(ec, ct, create_or_alter: create_or_alter);
                await CreateSchemaAndDictionary<ImportProcessStatus>(ec, ct, create_or_alter: create_or_alter);

                await CreateCustomProcs<ImportProcess>(null, ec, ct);
                await CreateCustomProcs<ImportProcessErrors>(null, ec, ct);
                await CreateCustomProcs<ImportProcessStatus>(null, ec, ct);

                // MMC: add to data dictionary
                await AddToDataDictionary<Objects>(ec, ct);
                await AddToDataDictionary<Numbering>(ec, ct);

            }
            finally
            {
                if (should_close) await ec.Disconnect();
            }
        }

        public static Dictionary<string, Type> GetCoreEntitiesTypes()
        {
            Dictionary<string, Type> result = [];
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
            bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);

            try
            {
                await ec.ExecuteSQLNonQuery(GrantExecutionToAllProcs<Categories>(login_or_group), ct);
                await ec.ExecuteSQLNonQuery(GrantExecutionToAllProcs<CategoriesValues>(login_or_group), ct);
                await ec.ExecuteSQLNonQuery(GrantExecutionToAllProcs<Status>(login_or_group), ct);
                await ec.ExecuteSQLNonQuery(GrantExecutionToAllProcs<StatusValues>(login_or_group), ct);
                await ec.ExecuteSQLNonQuery(GrantExecutionToAllProcs<MicromUsers>(login_or_group), ct);
                await ec.ExecuteSQLNonQuery(GrantExecutionToAllProcs<MicromUsersCat>(login_or_group), ct);
                await ec.ExecuteSQLNonQuery(GrantExecutionToAllProcs<MicromUsersDevices>(login_or_group), ct);
                await ec.ExecuteSQLNonQuery(GrantExecutionToAllProcs<MicromUsersGroups>(login_or_group), ct);
                await ec.ExecuteSQLNonQuery(GrantExecutionToAllProcs<MicromUsersGroupsMembers>(login_or_group), ct);
                await ec.ExecuteSQLNonQuery(GrantExecutionToAllProcs<MicromUsersGroupsMenus>(login_or_group), ct);

                await ec.ExecuteSQLNonQuery(GrantExecutionToAllProcs<FileStoreProcess>(login_or_group), ct);
                await ec.ExecuteSQLNonQuery(GrantExecutionToAllProcs<FileStore>(login_or_group), ct);
                await ec.ExecuteSQLNonQuery(GrantExecutionToAllProcs<FileStoreStatus>(login_or_group), ct);

                await ec.ExecuteSQLNonQuery(GrantExecutionToAllProcs<EmailServiceConfiguration>(login_or_group), ct);
                await ec.ExecuteSQLNonQuery(GrantExecutionToAllProcs<EmailServiceQueue>(login_or_group), ct);
                await ec.ExecuteSQLNonQuery(GrantExecutionToAllProcs<EmailServiceQueueStatus>(login_or_group), ct);
                await ec.ExecuteSQLNonQuery(GrantExecutionToAllProcs<EmailServiceTemplates>(login_or_group), ct);

                await ec.ExecuteSQLNonQuery(GrantExecutionToAllProcs<ImportProcess>(login_or_group), ct);
                await ec.ExecuteSQLNonQuery(GrantExecutionToAllProcs<ImportProcessErrors>(login_or_group), ct);
                await ec.ExecuteSQLNonQuery(GrantExecutionToAllProcs<ImportProcessStatus>(login_or_group), ct);
            }
            finally
            {
                if (should_close) await ec.Disconnect();
            }
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

    }
}
