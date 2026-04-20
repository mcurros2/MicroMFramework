using MicroM.Configuration;
using MicroM.Configuration.Entities;
using MicroM.Core;
using MicroM.Data;
using MicroM.DataDictionary.CategoriesDefinitions;
using MicroM.Extensions;
using static MicroM.Database.DataDictionarySchema;

namespace MicroM.Database;

public static class ConfigurationDatabaseSchema
{

    public static Dictionary<string, Type> GetCoreConfigurationEntitiesTypes()
    {
        Dictionary<string, Type> result = [];
        result.TryAddType<EntitiesAssemblies>();
        result.TryAddType<EntitiesAssembliesTypes>();
        result.TryAddType<Applications>();
        result.TryAddType<ApplicationsCat>();
        result.TryAddType<ApplicationsAssemblies>();
        result.TryAddType<ApplicationAssemblyTypes>();
        result.TryAddType<ApplicationsUrls>();
        result.TryAddType<ApplicationOidcConfiguration>();
        result.TryAddType<MicromApplicationApiKeys>();
        result.TryAddType<MicromApplicationCertificates>();
        result.TryAddType<ApplicationOidcClients>();
        result.TryAddType<ApplicationOidcClientsAuthorizedUrls>();
        result.TryAddType<ApplicationAdConfiguration>();

        return result;
    }

    public static CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> GetConfigurationEntitiesInstances(IEntityClient? ec = null, string? schema_name = null)
    {
        CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> result = new();// GetDataDictionaryEntitiesInstances(ec, schema_name);

        result.TryAddEntities(create_or_alter: true, entities: [
            ec == null ? new EntitiesAssemblies(schema_name) : new EntitiesAssemblies(ec, schema_name: schema_name),
            ec == null ? new EntitiesAssembliesTypes(schema_name) : new EntitiesAssembliesTypes(ec, schema_name: schema_name),
            ec == null ? new Applications(schema_name) : new Applications(ec, schema_name: schema_name),
            ec == null ? new ApplicationsCat(schema_name) : new ApplicationsCat(ec, schema_name: schema_name),
            ec == null ? new ApplicationsAssemblies(schema_name) : new ApplicationsAssemblies(ec, schema_name: schema_name),
            ec == null ? new ApplicationAssemblyTypes(schema_name) : new ApplicationAssemblyTypes(ec, schema_name: schema_name),
            ec == null ? new ApplicationsUrls(schema_name) : new ApplicationsUrls(ec, schema_name: schema_name),
            ec == null ? new ApplicationOidcConfiguration(schema_name) : new ApplicationOidcConfiguration(ec, schema_name: schema_name),
            ec == null ? new MicromApplicationApiKeys(schema_name) : new MicromApplicationApiKeys(ec, schema_name: schema_name),
            ec == null ? new MicromApplicationCertificates(schema_name) : new MicromApplicationCertificates(ec, schema_name: schema_name),
            ec == null ? new ApplicationOidcClients(schema_name) : new ApplicationOidcClients(ec, schema_name: schema_name),
            ec == null ? new ApplicationOidcClientsAuthorizedUrls(schema_name) : new ApplicationOidcClientsAuthorizedUrls(ec, schema_name: schema_name),
            ec == null ? new ApplicationAdConfiguration(schema_name) : new ApplicationAdConfiguration(ec, schema_name: schema_name),
            ]);

        return result;
    }


    public async static Task<CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>>?> CreateConfigurationDBSchemaAndProcs(IEntityClient ec, AppDBSchemaConfiguration schema_config, CancellationToken ct, bool create_or_alter = false)
    {
        bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);

        CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>>? dd_entities = null;
        CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>>? cfg_entities = null;
        try
        {
            await ec.Connect(ct);

            // This will also create the schema_name. If for any reason you want to have tweo schemas here you will need to take care of creating the schema yourself
            dd_entities = await CreateDatadictionarySchemaAndProcs(ec, schema_config, ct, create_or_alter);

            cfg_entities = GetConfigurationEntitiesInstances(ec, schema_config.APPSchema);

            // Get all custom procs in the assembly (this will get DataDictionary and Configuration
            var custom_procs_assembly = (cfg_entities[0]?.EntityType.Assembly) ?? throw new InvalidOperationException("Unable to determine the assembly for custom procedures.");
            var custom_procs = await custom_procs_assembly.GetAllClassifiedCustomSQLScripts(ct, schema_name: schema_config.APPSchema);

            // Filter custom_procs based on the configuration entities mnemonic code (Mneo) to ensure only relevant procedures are included and return a new CustomOrderedDictionary<CustomScript> with the filtered procedures
            var cfg_custom_procs = custom_procs.Filter(cfg_entities);

            await cfg_entities.CreateSchemaAndProcs(ec, schema_config, ct, create_or_alter, cfg_custom_procs);

            await CreateCategory<AuthenticationTypes>(ec, ct, schema_config.DDSchema);
            await CreateCategory<IdentityProviderRole>(ec, ct, schema_config.DDSchema);

            await cfg_entities.AddEntitiesToDataDictionary(ec, ct, schema_config.DDSchema);

        }
        finally
        {
            if (dd_entities?.Count > 0) dd_entities.Clear();
            if (should_close) await ec.Disconnect();
        }

        return cfg_entities;
    }


}
