using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;
using System.Data;

namespace MicroM.DataDictionary.Entities;

public class ApplicationOidcActiveSessionsDef : EntityDefinition
{
    public ApplicationOidcActiveSessionsDef() : base("aos", nameof(ApplicationOidcActiveSessions)) { }

    public readonly Column<string> c_application_id = Column<string>.PK();
    public readonly Column<string> vc_username = Column<string>.PK();
    public readonly Column<string> c_device_id = Column<string>.PK();
    public readonly Column<string> c_session_id = Column<string>.PK(autonum: true);

    public readonly Column<Guid> ui_oidc_session_guid_id = new();

    public readonly Column<string?> vc_oidc_refreshtoken = new(sql_type: SqlDbType.VarChar, size: 255, nullable: true, encrypted: true);
    public readonly Column<DateTime?> dt_refresh_expiration = new(nullable: true);

    public readonly ProcedureDefinition aos_deleteUserSessions = new(nameof(vc_username));
    public readonly ProcedureDefinition aos_deleteAllSessions = new();
    public readonly ProcedureDefinition aos_deleteSessionGUID = new(nameof(ui_oidc_session_guid_id));

    // No referential integrity for application. The applications table is only created in the configuration db
    // users and devices are left out intentionally to be orphaned if the user or device is deleted
    // When acting as IdPServer sessions are maintained here
    // When acting as a client, sessions are maintained to link with IdP.

    public readonly EntityForeignKey<MicromUsers, ApplicationOidcActiveSessions> FKUsers = new();

    public readonly EntityUniqueConstraint UNApplicationSession = new(keys: [nameof(ui_oidc_session_guid_id)]);
}

public class ApplicationOidcActiveSessions : Entity<ApplicationOidcActiveSessionsDef>
{
    public ApplicationOidcActiveSessions() : base() { }
    public ApplicationOidcActiveSessions(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

}
