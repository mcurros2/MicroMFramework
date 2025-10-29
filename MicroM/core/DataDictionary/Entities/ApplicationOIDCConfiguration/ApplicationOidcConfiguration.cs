using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary.Entities;

public class ApplicationOidcConfigurationDef : EntityDefinition
{
    public ApplicationOidcConfigurationDef() : base("aoc", nameof(ApplicationOidcConfiguration)) { SQLCreationOptions = SQLCreationOptionsMetadata.WithIUpdate; }

    public readonly Column<string> c_application_id = Column<string>.PK();

    // if acting as a server, this is the certificate to use for signing tokens
    // if acting as a client, this is the certificate to use for client authentication (private key jwt)
    public readonly Column<string?> c_certificate_id = Column<string?>.FK(nullable: true);

    // if acting as a client, this is the URL to the OIDC well-known configuration
    public readonly Column<string?> vc_url_wellknown = Column<string?>.Text(size: 2048, nullable: true);

    // if login in to IdP app this is the subject pepper to use when creating subject claim
    public readonly Column<string?> vc_oidc_idp_subject_pepper = Column<string?>.Text(size: 2048, encrypted: true, nullable: true);

    public readonly EntityForeignKey<MicromApplicationCertificates, ApplicationOidcConfiguration> FKApplicationCertificates = new();
}

public class ApplicationOidcConfiguration : Entity<ApplicationOidcConfigurationDef>
{
    public ApplicationOidcConfiguration() : base() { }
    public ApplicationOidcConfiguration(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

}


