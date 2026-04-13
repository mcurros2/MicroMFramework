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

    public static CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> GetConfigurationEntitiesInstances(IEntityClient? ec = null)
    {
        CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> result = GetDataDictionaryEntitiesInstances(ec);

        result.TryAddEntities(create_or_alter: true, entities: [
            ec == null ? new EntitiesAssemblies() : new EntitiesAssemblies(ec),
            ec == null ? new EntitiesAssembliesTypes() : new EntitiesAssembliesTypes(ec),
            ec == null ? new Applications() : new Applications(ec),
            ec == null ? new ApplicationsCat() : new ApplicationsCat(ec),
            ec == null ? new ApplicationsAssemblies() : new ApplicationsAssemblies(ec),
            ec == null ? new ApplicationAssemblyTypes() : new ApplicationAssemblyTypes(ec),
            ec == null ? new ApplicationsUrls() : new ApplicationsUrls(ec),
            ec == null ? new ApplicationOidcConfiguration() : new ApplicationOidcConfiguration(ec),
            ec == null ? new MicromApplicationApiKeys() : new MicromApplicationApiKeys(ec),
            ec == null ? new MicromApplicationCertificates() : new MicromApplicationCertificates(ec),
            ec == null ? new ApplicationOidcClients() : new ApplicationOidcClients(ec),
            ec == null ? new ApplicationOidcClientsAuthorizedUrls() : new ApplicationOidcClientsAuthorizedUrls(ec),
            ec == null ? new ApplicationAdConfiguration() : new ApplicationAdConfiguration(ec),
            ]);

        return result;
    }


    public async static Task CreateConfigurationDBSchemaAndProcs(IEntityClient ec, CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> entities, CancellationToken ct, bool create_or_alter = false)
    {
        bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);
        try
        {
            await ec.Connect(ct);
            await CreateDatadictionarySchemaAndProcs(ec, ct, create_or_alter);
            await CreateCategory<AuthenticationTypes>(ec, ct);
            await CreateSchemaAndDictionary<EntitiesAssemblies>(ec, ct, create_custom_procs: true, create_or_alter: create_or_alter);
            await CreateSchemaAndDictionary<EntitiesAssembliesTypes>(ec, ct, create_custom_procs: true, create_or_alter: create_or_alter);
            await CreateSchemaAndDictionary<Applications>(ec, ct, create_custom_procs: true, create_or_alter: create_or_alter);
            await CreateSchemaAndDictionary<ApplicationsCat>(ec, ct, create_custom_procs: true, create_or_alter: create_or_alter);
            await CreateSchemaAndDictionary<ApplicationsAssemblies>(ec, ct, create_custom_procs: true, create_or_alter: create_or_alter);
            await CreateSchemaAndDictionary<ApplicationAssemblyTypes>(ec, ct, create_custom_procs: true, create_or_alter: create_or_alter);
            await CreateSchemaAndDictionary<ApplicationsUrls>(ec, ct, create_custom_procs: true, create_or_alter: create_or_alter);
            await CreateSchemaAndDictionary<MicromApplicationCertificates>(ec, ct, create_custom_procs: true, create_or_alter: create_or_alter);
            await CreateSchemaAndDictionary<ApplicationOidcConfiguration>(ec, ct, create_custom_procs: true, create_or_alter: create_or_alter);
            await CreateSchemaAndDictionary<MicromApplicationApiKeys>(ec, ct, create_custom_procs: true, create_or_alter: create_or_alter);
            await CreateSchemaAndDictionary<ApplicationOidcClients>(ec, ct, create_custom_procs: true, create_or_alter: create_or_alter);
            await CreateSchemaAndDictionary<ApplicationOidcClientsAuthorizedUrls>(ec, ct, create_custom_procs: true, create_or_alter: create_or_alter);
            await CreateSchemaAndDictionary<ApplicationAdConfiguration>(ec, ct, create_custom_procs: true, create_or_alter: create_or_alter);
        }
        finally
        {
            if (should_close) await ec.Disconnect();
        }
    }


}
