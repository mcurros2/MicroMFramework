using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary;

public class ApplicationOidcClientsDef : EntityDefinition
{
    public ApplicationOidcClientsDef() : base("aoic", nameof(ApplicationOidcClients)) { }

    public readonly Column<string> c_application_id = Column<string>.PK();
    public readonly Column<string> c_client_app_id = Column<string>.PK();
    public readonly Column<string> c_user_id = Column<string>.FK();

    public readonly Column<string> vc_url_sso_root = Column<string>.Text(size: 2048);
    public readonly Column<string> vc_url_sso_login = Column<string>.Text(size: 2048);
    public readonly Column<string> vc_url_sso_backchannel_logout = Column<string>.Text(size: 2048);
    public readonly Column<string> vc_url_sso_logged_out = Column<string>.Text(size: 2048);

    public readonly EntityForeignKey<Applications, ApplicationOidcClients> FKApplicationsClients = new();
    public readonly EntityForeignKey<MicromUsers, ApplicationOidcClients> FKApplicationClientsUser = new();

}

public class ApplicationOidcClients : Entity<ApplicationOidcClientsDef>
{
    public ApplicationOidcClients() : base() { }
    public ApplicationOidcClients(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

}


