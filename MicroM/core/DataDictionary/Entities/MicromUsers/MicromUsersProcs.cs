using MicroM.Data;

namespace MicroM.DataDictionary.Procs;

public class usr_updateLoginAttempt : ProcedureDefinition
{
    public usr_updateLoginAttempt() : base() { }

    public readonly Column<string> c_user_id = Column<string>.PK();
    public readonly Column<string?> new_refresh_token = Column<string?>.Text(nullable: true);
    public readonly Column<bool> success = new();
    public readonly Column<int> account_lockout_mins = new();
    public readonly Column<int> refresh_expiration_hours = new();
    public readonly Column<int> max_bad_logon_attempts = new();
    public readonly Column<string> device_id = Column<string>.Text();
    public readonly Column<string> user_agent = Column<string>.Text(size: 4096);
    public readonly Column<string> ipaddress = Column<string>.Text(size: 40);

}

public class usr_getUserData : ProcedureDefinition
{
    public usr_getUserData() : base(readonly_locks: true) { }

    public readonly Column<string> vc_username = Column<string>.Text();
    public readonly Column<string> c_user_id = Column<string>.PK();
    public readonly Column<string> device_id = Column<string>.Text();
}

