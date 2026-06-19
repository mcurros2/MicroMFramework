using MicroM.Data;

namespace MicroM.DataDictionary.Procs;

public class usd_refreshToken : ProcedureDefinition
{
    public usd_refreshToken() : base() { }

    public readonly Column<string> c_user_id = Column<string>.Text();
    public readonly Column<string> c_device_id = Column<string>.Text();
    public readonly Column<string?> vc_refreshtoken = Column<string?>.Text(nullable: true);
    public readonly Column<string> new_refresh_token = Column<string>.Text();
    public readonly Column<int> refresh_expiration_hours = new();
    public readonly Column<int> max_refresh_count = new();
}
