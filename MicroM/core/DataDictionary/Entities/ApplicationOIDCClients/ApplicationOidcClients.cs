using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary;

/// <summary>
/// Schema definition for OIDC client applications associated with a MicroM application.
/// </summary>
public class ApplicationOidcClientsDef : EntityDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationOidcClientsDef"/> class.
    /// </summary>
    public ApplicationOidcClientsDef() : base("aoic", nameof(ApplicationOidcClients)) { }

    /// <summary>
    /// Application identifier.
    /// </summary>
    public readonly Column<string> c_application_id = Column<string>.PK();

    /// <summary>
    /// Client application identifier.
    /// </summary>
    public readonly Column<string> c_client_app_id = Column<string>.PK();

    /// <summary>
    /// User identifier associated with the client.
    /// </summary>
    public readonly Column<string> c_user_id = Column<string>.FK();

    /// <summary>
    /// Root URL for the SSO client.
    /// </summary>
    public readonly Column<string> vc_url_sso_root = Column<string>.Text(size: 2048);

    /// <summary>
    /// Login URL for the SSO client.
    /// </summary>
    public readonly Column<string> vc_url_sso_login = Column<string>.Text(size: 2048);

    /// <summary>
    /// Back-channel logout URL for the SSO client.
    /// </summary>
    public readonly Column<string> vc_url_sso_backchannel_logout = Column<string>.Text(size: 2048);

    /// <summary>
    /// Logged-out redirect URL for the SSO client.
    /// </summary>
    public readonly Column<string> vc_url_sso_logged_out = Column<string>.Text(size: 2048);

    /// <summary>
    /// Foreign key relation to the parent application.
    /// </summary>
    public readonly EntityForeignKey<Applications, ApplicationOidcClients> FKApplicationsClients = new();

    /// <summary>
    /// Foreign key relation to the user owning the client.
    /// </summary>
    public readonly EntityForeignKey<MicromUsers, ApplicationOidcClients> FKApplicationClientsUser = new();

}

/// <summary>
/// Entity for managing OIDC client registrations for applications.
/// </summary>
public class ApplicationOidcClients : Entity<ApplicationOidcClientsDef>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationOidcClients"/> class.
    /// </summary>
    public ApplicationOidcClients() : base() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationOidcClients"/> class with the specified client and encryption provider.
    /// </summary>
    /// <param name="ec">Entity client used for data access.</param>
    /// <param name="encryptor">Optional encryption provider.</param>
    public ApplicationOidcClients(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

}


