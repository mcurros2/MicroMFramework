using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;
using System.Data;

namespace MicroM.DataDictionary
{
    public class MicromUsersDevicesDef : EntityDefinition
    {
        public MicromUsersDevicesDef() : base("usd", nameof(MicromUsersDevices)) { }

        public readonly Column<string> c_user_id = Column<string>.PK();
        public readonly Column<string> c_device_id = Column<string>.PK(sql_type: SqlDbType.VarChar, size: 255);
        public readonly Column<string?> vc_useragent = new(sql_type: SqlDbType.VarChar, size: 4096, nullable: true);
        public readonly Column<string?> vc_ipaddress = new(sql_type: SqlDbType.VarChar, size: 40, nullable: true);
        public readonly Column<string> vc_refreshtoken = new(sql_type: SqlDbType.VarChar, size: 255);
        public readonly Column<DateTime> dt_refresh_expiration = new();
        public readonly Column<int> i_refreshcount = new(value: 0);

        public ViewDefinition usd_brwStandard { get; private set; } = new(nameof(c_user_id), nameof(c_device_id));

        public readonly EntityForeignKey<MicromUsers, MicromUsersDevices> FKMicromUsers = new();

        public ProcedureDefinition usd_refreshToken { get; private set; } = null!;

        protected override void DefineProcs()
        {
            // MMC: this declaration are just to be used as constants for parameter names using nameof
            string refresh_expiration_hours, new_refresh_token, max_refresh_count;

            usd_refreshToken = new ProcedureDefinition();
            usd_refreshToken.AddParmFromCol<string>(c_user_id);
            usd_refreshToken.AddParmFromCol<string>(c_device_id);
            usd_refreshToken.AddParmFromCol<string>(vc_refreshtoken);
            usd_refreshToken.AddParm<string>(nameof(new_refresh_token), sql_type: SqlDbType.VarChar, size: 255);
            usd_refreshToken.AddParm<int>(nameof(refresh_expiration_hours), sql_type: SqlDbType.Int);
            usd_refreshToken.AddParm<int>(nameof(max_refresh_count), sql_type: SqlDbType.Int);

       }
    }

    public class MicromUsersDevices : Entity<MicromUsersDevicesDef>
    {
        public MicromUsersDevices() : base() { }
        public MicromUsersDevices(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }
    }
}
