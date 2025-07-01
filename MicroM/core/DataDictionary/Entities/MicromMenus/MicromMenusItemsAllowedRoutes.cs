
using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary
{

    public class MicromMenusItemsRoutesDef : EntityDefinition
    {
        public MicromMenusItemsRoutesDef() : base("mir", nameof(MicromMenusItemsAllowedRoutes)) { SQLCreationOptions = SQLCreationOptionsMetadata.WithIUpdate; }

        public readonly Column<string> c_menu_id = Column<string>.PK(size: 50);
        public readonly Column<string> c_menu_item_id = Column<string>.PK(size: 50);

        // MMC: this column is not autonum, is the way to return the route id, see custom mir_update
        public readonly Column<string> c_route_id = Column<string>.PK(autonum: true);

        public readonly Column<string> vc_route_path = Column<string>.Text(size: 2048, fake: true);

        public readonly ViewDefinition mir_brwStandard = new(nameof(c_menu_id));

        public readonly EntityForeignKey<MicromMenusItems, MicromMenusItemsAllowedRoutes> FKMenuItems = new();
        public readonly EntityForeignKey<MicromRoutes, MicromMenusItemsAllowedRoutes> FKRoutes = new();


    }

    public class MicromMenusItemsAllowedRoutes : Entity<MicromMenusItemsRoutesDef>
    {
        public MicromMenusItemsAllowedRoutes() : base() { }
        public MicromMenusItemsAllowedRoutes(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

    }


}
