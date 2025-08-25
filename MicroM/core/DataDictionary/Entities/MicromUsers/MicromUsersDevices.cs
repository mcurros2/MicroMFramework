using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;
using System.Data;

namespace MicroM.DataDictionary
{
    /// <summary>
    /// Schema definition for devices associated with MicroM users.
    /// </summary>
    public class MicromUsersDevicesDef : EntityDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MicromUsersDevicesDef"/> class.
        /// </summary>
        public MicromUsersDevicesDef() : base("usd", nameof(MicromUsersDevices)) { SQLCreationOptions = SQLCreationOptionsMetadata.WithIUpdate; }

        /// <summary>User identifier.</summary>
        public readonly Column<string> c_user_id = Column<string>.PK();
        /// <summary>Device identifier.</summary>
        public readonly Column<string> c_device_id = Column<string>.PK(sql_type: SqlDbType.VarChar, size: 255);
        /// <summary>User agent string reported by the device.</summary>
        public readonly Column<string?> vc_useragent = new(sql_type: SqlDbType.VarChar, size: 4096, nullable: true);
        /// <summary>IP address of the device.</summary>
        public readonly Column<string?> vc_ipaddress = new(sql_type: SqlDbType.VarChar, size: 40, nullable: true);
        /// <summary>Refresh token assigned to the device.</summary>
        public readonly Column<string?> vc_refreshtoken = new(sql_type: SqlDbType.VarChar, size: 255, nullable: true);
        /// <summary>Expiration time of the refresh token.</summary>
        public readonly Column<DateTime?> dt_refresh_expiration = new(nullable: true);
        /// <summary>Number of times the token has been refreshed.</summary>
        public readonly Column<int> i_refreshcount = new(value: 0);

        /// <summary>Default browse view definition.</summary>
        public ViewDefinition usd_brwStandard { get; private set; } = new(nameof(c_user_id), nameof(c_device_id));

        /// <summary>Relationship to the owning user.</summary>
        public readonly EntityForeignKey<MicromUsers, MicromUsersDevices> FKMicromUsers = new();

        /// <summary>Procedure to refresh a device token.</summary>
        public ProcedureDefinition usd_refreshToken { get; private set; } = null!;

        protected override void DefineProcs()
        {
            // MMC: this declaration are just to be used as constants for parameter names using nameof
            string refresh_expiration_hours, new_refresh_token, max_refresh_count;

            usd_refreshToken = new ProcedureDefinition();
            usd_refreshToken.AddParmFromCol<string>(c_user_id);
            usd_refreshToken.AddParmFromCol<string>(c_device_id);
            usd_refreshToken.AddParmFromCol<string?>(vc_refreshtoken);
            usd_refreshToken.AddParm<string>(nameof(new_refresh_token), sql_type: SqlDbType.VarChar, size: 255);
            usd_refreshToken.AddParm<int>(nameof(refresh_expiration_hours), sql_type: SqlDbType.Int);
            usd_refreshToken.AddParm<int>(nameof(max_refresh_count), sql_type: SqlDbType.Int);

        }
    }

    /// <summary>
    /// Entity for managing device records linked to users.
    /// </summary>
    public class MicromUsersDevices : Entity<MicromUsersDevicesDef>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MicromUsersDevices"/> class.
        /// </summary>
        public MicromUsersDevices() : base() { }
        /// <summary>
        /// Initializes a new instance with a database client and optional encryptor.
        /// </summary>
        /// <param name="ec">Entity client.</param>
        /// <param name="encryptor">Optional encryptor.</param>
        public MicromUsersDevices(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }
    }
}
