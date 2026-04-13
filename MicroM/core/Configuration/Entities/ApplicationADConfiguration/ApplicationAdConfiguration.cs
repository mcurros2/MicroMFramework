using MicroM.Core;
using MicroM.Data;
using MicroM.DataDictionary.Entities;
using MicroM.Web.Services;

namespace MicroM.Configuration.Entities;

public class ApplicationAdConfigurationDef : EntityDefinition
{
    public ApplicationAdConfigurationDef() : base("aad", nameof(ApplicationAdConfiguration), schemaName: DataDefaults.DataDictionarySchema) { }

    public readonly Column<string> c_ad_configuration_id = Column<string>.PK(autonum: true);
    public readonly Column<string> c_application_id = Column<string>.FK();

    // this is the email suffix to use when doing user lookups in AD
    public readonly Column<string> vc_ad_domain = Column<string>.Text(size: 2048);
    public readonly Column<string> vc_user_principal_domain = Column<string>.Text(size: 2048);

    public readonly Column<string> vc_ad_container = Column<string>.Text(size: 2048);
    public readonly Column<string> vc_ad_server_ip = Column<string>.Text(size: 64);
    public readonly Column<string> vc_ad_user = Column<string>.Text(size: 256, encrypted: true);
    public readonly Column<string> vc_ad_password = Column<string>.Text(size: 2048, encrypted: true);

    public readonly Column<bool> bt_create_user_on_login = new() { Value = false };

    public readonly Column<string?> c_default_user_group_id = Column<string?>.FK(nullable: true);

    public readonly ViewDefinition aad_brwStandard = new(nameof(c_application_id));

    public readonly EntityForeignKey<Applications, ApplicationAdConfiguration> FKApplication = new();
    public readonly EntityForeignKey<MicromUsersGroups, ApplicationAdConfiguration> FKUserGroups = new(fake: true, key_mappings: [new(nameof(MicromUsersGroupsDef.c_user_group_id), nameof(c_default_user_group_id))]);

    public readonly EntityUniqueConstraint UNPrincipalDomain = new(keys: [nameof(c_application_id), nameof(vc_user_principal_domain)]);
}

public class ApplicationAdConfiguration : Entity<ApplicationAdConfigurationDef>
{
    public ApplicationAdConfiguration() : base() { }
    public ApplicationAdConfiguration(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }
}

