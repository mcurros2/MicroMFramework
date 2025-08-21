using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary;

/// <summary>
/// Schema definition for OIDC server settings for an application.
/// </summary>
public class ApplicationOidcServerDef : EntityDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationOidcServerDef"/> class.
    /// </summary>
    public ApplicationOidcServerDef() : base("aois", nameof(ApplicationOidcServer)) { SQLCreationOptions = SQLCreationOptionsMetadata.WithIUpdate; }

    /// <summary>
    /// Application identifier.
    /// </summary>
    public readonly Column<string> c_application_id = Column<string>.PK();

    /// <summary>
    /// Well-known configuration URL.
    /// </summary>
    public readonly Column<string?> vc_url_wellknown = Column<string?>.Text(size: 2048, nullable: true);

    /// <summary>
    /// JWKS endpoint URL when acting as an identity provider.
    /// </summary>
    public readonly Column<string?> vc_url_jwks = Column<string?>.Text(size: 2048, nullable: true);

    /// <summary>
    /// Authorization endpoint URL when acting as an identity provider.
    /// </summary>
    public readonly Column<string?> vc_url_authorize = Column<string?>.Text(size: 2048, nullable: true);

    /// <summary>
    /// Token endpoint URL for back-channel requests.
    /// </summary>
    public readonly Column<string?> vc_url_token_backchannel = Column<string?>.Text(size: 2048, nullable: true);

    /// <summary>
    /// End session endpoint URL.
    /// </summary>
    public readonly Column<string?> vc_url_endsession = Column<string?>.Text(size: 2048, nullable: true);

    /// <summary>
    /// Foreign key relation to the parent application.
    /// </summary>
    public readonly EntityForeignKey<Applications, ApplicationOidcServer> FKApplicationsClients = new();
}

/// <summary>
/// Entity for managing OIDC server configuration for applications.
/// </summary>
public class ApplicationOidcServer : Entity<ApplicationOidcServerDef>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationOidcServer"/> class.
    /// </summary>
    public ApplicationOidcServer() : base() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationOidcServer"/> class with the specified client and encryption provider.
    /// </summary>
    /// <param name="ec">Entity client used for data access.</param>
    /// <param name="encryptor">Optional encryption provider.</param>
    public ApplicationOidcServer(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

}


