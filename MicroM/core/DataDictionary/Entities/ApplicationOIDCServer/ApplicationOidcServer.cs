using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary;

public class ApplicationOidcServerDef : EntityDefinition
{
    public ApplicationOidcServerDef() : base("aois", nameof(ApplicationOidcServer)) { SQLCreationOptions = SQLCreationOptionsMetadata.WithIUpdate; }

    public readonly Column<string> c_application_id = Column<string>.PK();

    public readonly Column<string?> vc_url_wellknown = Column<string?>.Text(size: 2048, nullable: true);

    // if acting as an IDP, these are the URLs to the OIDC endpoints
    public readonly Column<string?> vc_url_jwks = Column<string?>.Text(size: 2048, nullable: true);
    public readonly Column<string?> vc_url_authorize = Column<string?>.Text(size: 2048, nullable: true);
    public readonly Column<string?> vc_url_token_backchannel = Column<string?>.Text(size: 2048, nullable: true);
    public readonly Column<string?> vc_url_endsession = Column<string?>.Text(size: 2048, nullable: true);

    public readonly EntityForeignKey<Applications, ApplicationOidcServer> FKApplicationsClients = new();
}

public class ApplicationOidcServer : Entity<ApplicationOidcServerDef>
{
    public ApplicationOidcServer() : base() { }
    public ApplicationOidcServer(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

}


