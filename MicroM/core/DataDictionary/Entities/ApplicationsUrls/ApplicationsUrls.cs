using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary
{
    public class ApplicationsUrlsDef : EntityDefinition
    {
        public ApplicationsUrlsDef() : base("apu", nameof(ApplicationsUrls)) { }

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
        public ApplicationsUrls(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

    }

}
