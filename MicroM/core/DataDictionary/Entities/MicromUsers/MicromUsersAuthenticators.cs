using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary.Entities;

public class MicromUsersAuthenticatorsDef : EntityDefinition
{
    public MicromUsersAuthenticatorsDef() : base("uau", nameof(MicromUsers)) { SQLCreationOptions = SQLCreationOptionsMetadata.WithIUpdateAndIDrop; }

    public readonly Column<string> c_user_id = Column<string>.PK();
    public readonly Column<string> c_authenticator_id = Column<string>.PK(autonum: true);
    public readonly Column<string> vc_authenticator_name = Column<string>.Text();
    public readonly Column<string?> vc_totp_secret = Column<string?>.Text(size: 2048, encrypted: true, nullable: true);

    public readonly ViewDefinition uau_brwStandard = new(nameof(c_user_id), nameof(c_authenticator_id));

    public readonly EntityForeignKey<MicromUsers, MicromUsersAuthenticators> FKMicromUsers = new();
}

public class MicromUsersAuthenticators : Entity<MicromUsersAuthenticatorsDef>
{
    public MicromUsersAuthenticators() : base() { }
    public MicromUsersAuthenticators(string? schema_name) : base(schema_name) { }
    public MicromUsersAuthenticators(IEntityClient ec, IMicroMEncryption? encryptor = null, string? schema_name = null) : base(ec, encryptor, schema_name) { }

}
