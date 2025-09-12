using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary.Entities;

public class ApplicationOidcClientsAuthorizedUrlsDef : EntityDefinition
{
    public ApplicationOidcClientsAuthorizedUrlsDef() : base("aou", nameof(ApplicationOidcClientsAuthorizedUrls)) { }

    public readonly Column<string> c_application_id = Column<string>.PK();
    public readonly Column<string> c_client_app_id = Column<string>.PK();
    public readonly Column<string> c_client_app_url_id = Column<string>.PK(autonum: true);

    public readonly Column<string> vc_authorized_url = Column<string>.Text(size: 2048);

    public readonly ViewDefinition aou_brwStandard = new(nameof(c_application_id), nameof(c_client_app_id), nameof(c_client_app_url_id));

    public readonly EntityForeignKey<ApplicationOidcConfiguration, ApplicationOidcClientsAuthorizedUrls> FKApplicationOidcConfiguration = new();
}

public class ApplicationOidcClientsAuthorizedUrls : Entity<ApplicationOidcClientsAuthorizedUrlsDef>
{
    public ApplicationOidcClientsAuthorizedUrls() : base() { }
    public ApplicationOidcClientsAuthorizedUrls(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }
}

