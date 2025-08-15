using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary;

public class ApplicationOidcServerSessionsDef : EntityDefinition
{
    public ApplicationOidcServerSessionsDef() : base("aoie", nameof(ApplicationOidcServerSessions)) { }
    public readonly Column<string> c_application_id = Column<string>.PK();
    public readonly Column<string> c_session_id = Column<string>.PK(autonum: true);
    public readonly Column<string> c_user_id = Column<string>.FK();

    public readonly Column<Guid> c_session_guid_id = new();

    public readonly EntityForeignKey<Applications, ApplicationOidcServerSessions> FKApplications = new();
    public readonly EntityForeignKey<MicromUsers, ApplicationOidcServerSessions> FKUsers = new();

    public readonly EntityUniqueConstraint UNApplicationSession = new(keys: [nameof(c_application_id), nameof(c_session_guid_id)]);
}

public class ApplicationOidcServerSessions : Entity<ApplicationOidcServerSessionsDef>
{
    public ApplicationOidcServerSessions() : base() { }
    public ApplicationOidcServerSessions(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }
}
