using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary.Entities;

public class ApplicationOidcActiveSessionsDef : EntityDefinition
{
    public ApplicationOidcActiveSessionsDef() : base("aos", nameof(ApplicationOidcActiveSessions)) { }

    public readonly Column<string> c_session_id = Column<string>.PK(autonum: true);
    public readonly Column<string> c_user_id = Column<string>.FK();

    public readonly Column<Guid> c_session_guid_id = new();

    public readonly ProcedureDefinition aos_deleteUserSessions = new(nameof(c_user_id));

    public readonly EntityForeignKey<MicromUsers, ApplicationOidcActiveSessions> FKUsers = new();

    public readonly EntityUniqueConstraint UNApplicationSession = new(keys: [nameof(c_session_guid_id)]);
}

public class ApplicationOidcActiveSessions : Entity<ApplicationOidcActiveSessionsDef>
{
    public ApplicationOidcActiveSessions() : base() { }
    public ApplicationOidcActiveSessions(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

    public async static Task<Guid> CreateActiveSession(IEntityClient ec, string user_id, CancellationToken ct)
    {
        ApplicationOidcActiveSessions new_session = new(ec);

        var session_guid = Guid.NewGuid();
        new_session.Def.c_user_id.Value = user_id;
        new_session.Def.c_session_guid_id.Value = session_guid;
        await new_session.InsertData(ct);

        return session_guid;
    }

    public async static Task DeleteActiveSessions(IEntityClient ec, string user_id, CancellationToken ct)
    {
        ApplicationOidcActiveSessions del_session = new(ec);
        del_session.Def.c_user_id.Value = user_id;
        await del_session.ExecuteProc(ct, del_session.Def.aos_deleteUserSessions);

        return;
    }
}
