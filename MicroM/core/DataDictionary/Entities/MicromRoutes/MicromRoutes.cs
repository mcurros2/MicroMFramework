using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary
{
    public class MicromRoutesDef : EntityDefinition
    {
        public MicromRoutesDef() : base("mro", nameof(MicromRoutes)) { }

        public readonly Column<string> c_route_id = Column<string>.PK(autonum: true);
        public readonly Column<string> vc_route_path = Column<string>.Text(size: 2048);

        public readonly ViewDefinition mro_brwStandard = new(nameof(c_route_id), nameof(vc_route_path));

        public readonly EntityUniqueConstraint UNRoutePath = new(keys: nameof(vc_route_path));

    }

    public class MicromRoutes : Entity<MicromRoutesDef>
    {
        public MicromRoutes() : base() { }
        public MicromRoutes(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

    }


}
