using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary
{
    /// <summary>
    /// Data dictionary definition for application URLs associated with an application.
    /// </summary>
    public class ApplicationsUrlsDef : EntityDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationsUrlsDef"/> class.
        /// </summary>
        public ApplicationsUrlsDef() : base("apu", nameof(ApplicationsUrls)) { SQLCreationOptions = SQLCreationOptionsMetadata.WithIUpdate; }

        /// <summary>
        /// Application identifier.
        /// </summary>
        public readonly Column<string> c_application_id = Column<string>.PK();

        /// <summary>
        /// Unique identifier for the application URL.
        /// </summary>
        public readonly Column<string> c_application_url_id = Column<string>.PK(autonum: true);

        /// <summary>
        /// Actual URL value.
        /// </summary>
        public readonly Column<string> vc_application_url = Column<string>.Text(size: 2048);

        /// <summary>
        /// Standard browse view definition for application URLs.
        /// </summary>
        public readonly ViewDefinition apu_brwStandard = new(nameof(c_application_id), nameof(c_application_url_id));

        /// <summary>
        /// Foreign key to the <see cref="Applications"/> entity.
        /// </summary>
        public readonly EntityForeignKey<Applications, ApplicationsUrls> FKApplicationsUrls = new();

        /// <summary>
        /// Ensures uniqueness of application URLs per application.
        /// </summary>
        public readonly EntityUniqueConstraint UNApplicationUrl = new(keys: [nameof(c_application_id), nameof(vc_application_url)]);
    }

    /// <summary>
    /// Runtime entity for interacting with application URLs.
    /// </summary>
    public class ApplicationsUrls : Entity<ApplicationsUrlsDef>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationsUrls"/> class.
        /// </summary>
        public ApplicationsUrls() : base() { }

        /// <summary>
        /// Initializes a new instance with a database client and optional encryptor.
        /// </summary>
        public ApplicationsUrls(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

    }

}
