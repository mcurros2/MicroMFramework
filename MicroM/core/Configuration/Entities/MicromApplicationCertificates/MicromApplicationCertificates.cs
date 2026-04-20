using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.Configuration.Entities;

public class MicromApplicationCertificatesDef : EntityDefinition
{
    public MicromApplicationCertificatesDef() : base("mac", nameof(MicromApplicationCertificates)) { SQLCreationOptions = SQLCreationOptionsMetadata.WithIUpdate; }

    public readonly Column<string> c_application_id = Column<string>.PK();
    public readonly Column<string> c_certificate_id = Column<string>.PK(autonum: true);

    public readonly Column<Guid> ui_certificate_guid_id = new();

    public readonly Column<byte[]> vb_certificate_blob = new(size: 0);
    public readonly Column<string> vc_certificate_password = Column<string>.Text(size: 2048, encrypted: true);

    public readonly ViewDefinition mac_brwStandard = new(nameof(c_application_id), nameof(c_certificate_id));

    public readonly EntityForeignKey<Applications, MicromApplicationCertificates> FKApplications = new();

    public readonly EntityUniqueConstraint UNApplicationCertificateGuid = new(keys: [nameof(ui_certificate_guid_id)]);
}

public class MicromApplicationCertificates : Entity<MicromApplicationCertificatesDef>
{
    public MicromApplicationCertificates() : base() { }
    public MicromApplicationCertificates(string? schema_name) : base(schema_name) { }
    public MicromApplicationCertificates(IEntityClient ec, IMicroMEncryption? encryptor = null, string? schema_name = null) : base(ec, encryptor, schema_name) { }

    public static (Guid guid, byte[] certificate, string password) CreateNewApplicationCertificate(string application_id)
    {
        var certificate_guid = Guid.NewGuid();
        using var new_certificate = CryptClass.CreateSelfSignedCertificate(distinguished_name: application_id);
        var certificate_password = CryptClass.CreateRandomPassword(length: 32);
        var pem = new_certificate.Export(System.Security.Cryptography.X509Certificates.X509ContentType.Pfx, certificate_password);
        return (certificate_guid, pem, certificate_password);
    }

    public override Task<DBStatusResult> InsertData(CancellationToken ct, bool throw_dbstat_exception = false, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IWebAPIServices? api = null, string? app_id = null)
    {
        var cert = CreateNewApplicationCertificate(this.Def.c_application_id.Value);

        Def.vc_certificate_password.Value = cert.password;
        Def.ui_certificate_guid_id.Value = cert.guid;
        Def.vb_certificate_blob.Value = cert.certificate;

        return base.InsertData(ct, throw_dbstat_exception, options, server_claims, api, app_id);
    }
}
