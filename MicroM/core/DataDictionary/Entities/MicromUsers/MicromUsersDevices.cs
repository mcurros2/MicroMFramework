using MicroM.Configuration;
using MicroM.Core;
using MicroM.Data;
using MicroM.DataDictionary.Procs;
using MicroM.Web.Services;


namespace MicroM.DataDictionary.Entities;

public class MicromUsersDevicesDef : EntityDefinition
{
    public MicromUsersDevicesDef() : base("usd", nameof(MicromUsersDevices), schemaName: DataDefaults.DataDictionarySchema) { SQLCreationOptions = SQLCreationOptionsMetadata.WithIUpdate; }

    public readonly Column<string> c_user_id = Column<string>.PK();
    public readonly Column<string> c_device_id = Column<string>.PK(size: 255);
    public readonly Column<string?> vc_useragent = Column<string?>.Text(size: 4096, nullable: true);
    public readonly Column<string?> vc_ipaddress = Column<string?>.Text(size: 40, nullable: true);
    public readonly Column<string?> vc_refreshtoken = Column<string?>.Text(nullable: true);
    public readonly Column<DateTime?> dt_refresh_expiration = new(nullable: true);
    public readonly Column<int> i_refreshcount = new(value: 0);

    public readonly ViewDefinition usd_brwStandard = new(nameof(c_user_id), nameof(c_device_id));

    public readonly EntityForeignKey<MicromUsers, MicromUsersDevices> FKMicromUsers = new();

    public readonly usd_refreshToken usd_refreshToken = new();


}

public class MicromUsersDevices : Entity<MicromUsersDevicesDef>
{
    public MicromUsersDevices() : base() { }
    public MicromUsersDevices(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }
}