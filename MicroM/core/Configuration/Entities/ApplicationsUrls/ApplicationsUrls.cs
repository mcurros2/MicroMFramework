using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.Configuration.Entities;

public class ApplicationsUrlsDef : EntityDefinition
{
    public ApplicationsUrlsDef() : base("apu", nameof(ApplicationsUrls)) { SQLCreationOptions = SQLCreationOptionsMetadata.WithIUpdate; }

    public readonly Column<string> c_application_id = Column<string>.PK();
    public readonly Column<string> c_application_url_id = Column<string>.PK(autonum: true);
    public readonly Column<string> vc_application_url = Column<string>.Text(size: 2048);

    public readonly ViewDefinition apu_brwStandard = new(nameof(c_application_id), nameof(c_application_url_id));

    public readonly EntityForeignKey<Applications, ApplicationsUrls> FKApplicationsUrls = new();

    public readonly EntityUniqueConstraint UNApplicationUrl = new(keys: [nameof(c_application_id), nameof(vc_application_url)]);
}

public class ApplicationsUrls : Entity<ApplicationsUrlsDef>
{
    public ApplicationsUrls() : base() { }
    public ApplicationsUrls(string? schema_name) : base(schema_name) { }
    public ApplicationsUrls(IEntityClient ec, IMicroMEncryption? encryptor = null, string? schema_name = null) : base(ec, encryptor, schema_name) { }

}
