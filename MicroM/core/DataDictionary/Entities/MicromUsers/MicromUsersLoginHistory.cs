using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary
{
    /// <summary>
    /// Schema definition for recording user login history.
    /// </summary>
    public class MicromUsersLoginHistoryDef : EntityDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MicromUsersLoginHistoryDef"/> class.
        /// </summary>
        public MicromUsersLoginHistoryDef() : base("ulh", nameof(MicromUsersLoginHistory)) { SQLCreationOptions = SQLCreationOptionsMetadata.WithIUpdate; }

        /// <summary>History record identifier.</summary>
        public readonly Column<string> c_user_history_id = Column<string>.PK(autonum: true);
        /// <summary>User identifier.</summary>
        public readonly Column<string> c_user_id = Column<string>.PK();

        /// <summary>User agent reported during login.</summary>
        public readonly Column<string?> vc_useragent = Column<string?>.Text(size: 4096, nullable: true);
        /// <summary>IP address from which the login was attempted.</summary>
        public readonly Column<string?> vc_ipaddress = Column<string?>.Text(size: 40, nullable: true);
        /// <summary>Indicates whether the login attempt succeeded.</summary>
        public readonly Column<bool> bt_success = new();
        /// <summary>Timestamp of the login attempt.</summary>
        public readonly Column<DateTime> dt_login_attempt = new();

        /// <summary>Default browse view definition.</summary>
        public ViewDefinition ulh_brwStandard { get; private set; } = new(nameof(c_user_id));

    }

    /// <summary>
    /// Entity for managing login history records.
    /// </summary>
    public class MicromUsersLoginHistory : Entity<MicromUsersLoginHistoryDef>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MicromUsersLoginHistory"/> class.
        /// </summary>
        public MicromUsersLoginHistory() : base() { }
        /// <summary>
        /// Initializes a new instance with a database client and optional encryptor.
        /// </summary>
        /// <param name="ec">Entity client.</param>
        /// <param name="encryptor">Optional encryptor.</param>
        public MicromUsersLoginHistory(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

    }

}

