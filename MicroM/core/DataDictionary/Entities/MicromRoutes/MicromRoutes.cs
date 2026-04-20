using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary.Entities;

public class MicromRoutesDef : EntityDefinition
{
    public MicromRoutesDef() : base("mro", nameof(MicromRoutes)) { }

    public readonly Column<string> c_route_id = Column<string>.PK(autonum: true);
    public readonly Column<string> vc_route_path = Column<string>.Text(size: 2048);

    public readonly ViewDefinition mro_brwStandard = new(nameof(c_route_id), nameof(vc_route_path));

    public readonly ProcedureDefinition mro_deleteAllRoutes = new();

    public readonly EntityUniqueConstraint UNRoutePath = new(keys: nameof(vc_route_path));

}

public class MicromRoutes : Entity<MicromRoutesDef>
{
    public MicromRoutes() : base() { }
    public MicromRoutes(string? schema_name) : base(schema_name) { }
    public MicromRoutes(IEntityClient ec, IMicroMEncryption? encryptor = null, string? schema_name = null) : base(ec, encryptor, schema_name) { }

    public static async Task<DBStatusResult> DeleteAllRoutes(IEntityClient ec, string dd_schema, CancellationToken ct)
    {
        MicromRoutes routes = new(ec, schema_name: dd_schema);

        var proc = routes.Def.mro_deleteAllRoutes;

        var result = await routes.Data.ExecuteProcDBStatus(proc, ct, throw_dbstat_exception: true);

        return result;
    }

}
