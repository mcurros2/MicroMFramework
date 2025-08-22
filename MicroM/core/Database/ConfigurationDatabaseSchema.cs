using MicroM.Core;
using MicroM.Data;
using MicroM.DataDictionary;
using MicroM.DataDictionary.CategoriesDefinitions;
using static MicroM.Database.DataDictionarySchema;

namespace MicroM.Database;

public static class ConfigurationDatabaseSchema
{

    public static CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> GetConfigurationEntitiesTypes(IEntityClient ec)
    {
        CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> result = GetDataDictionaryEntitiesTypes(ec);

        result.TryAddEntities(create_or_alter: true, entities: [
            new EntitiesAssemblies(ec),
            new EntitiesAssembliesTypes(ec),
            new Applications(ec),
            new ApplicationsCat(ec),
            new ApplicationsAssemblies(ec),
            new ApplicationAssemblyTypes(ec),
            new ApplicationsUrls(ec),
            new ApplicationOidcClients(ec),
            new ApplicationOidcServer(ec),
            new ApplicationOidcServerSessions(ec)
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
            await CreateSchemaAndDictionary<ApplicationOidcClients>(ec, ct, create_custom_procs: true, create_or_alter: create_or_alter);
            await CreateSchemaAndDictionary<ApplicationOidcServer>(ec, ct, create_custom_procs: true, create_or_alter: create_or_alter);
            await CreateSchemaAndDictionary<ApplicationOidcServerSessions>(ec, ct, create_custom_procs: true, create_or_alter: create_or_alter);
        }
        finally
        {
            if (should_close) await ec.Disconnect();
        }
    }


}
