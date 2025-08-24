
using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary
{

    /// <summary>
    /// Defines the schema for MicroM menu records.
    /// </summary>
    public class MicromMenusDef : EntityDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MicromMenusDef"/> class.
        /// </summary>
        public MicromMenusDef() : base("mme", nameof(MicromMenus)) { }

        /// <summary>
        /// Primary key column for the menu identifier.
        /// </summary>
        public readonly Column<string> c_menu_id = Column<string>.PK(size: 50);

        /// <summary>
        /// Descriptive name of the menu.
        /// </summary>
        public readonly Column<string> vc_menu_name = Column<string>.Text();

        /// <summary>
        /// Timestamp of the last route update.
        /// </summary>
        public readonly Column<DateTime>? dt_last_route_updated = new(nullable: true);

        /// <summary>
        /// Standard browse view keyed by menu ID.
        /// </summary>
        public readonly ViewDefinition mme_brwStandard = new(nameof(c_menu_id));

    }

    /// <summary>
    /// Entity wrapper for working with <see cref="MicromMenusDef"/> records.
    /// </summary>
    public class MicromMenus : Entity<MicromMenusDef>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MicromMenus"/> class.
        /// </summary>
        public MicromMenus() : base() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="MicromMenus"/> class using the specified client and encryptor.
        /// </summary>
        /// <param name="ec">Entity client used for database access.</param>
        /// <param name="encryptor">Optional encryptor for sensitive data.</param>
        public MicromMenus(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

    }


}
