
using MicroM.Configuration;
using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary
{

    public class MicromUsersGroupsDef : EntityDefinition
    {
        public MicromUsersGroupsDef() : base("mug", nameof(MicromUsersGroups)) { }

        public readonly Column<string> c_user_group_id = Column<string>.PK(autonum: true);
        public readonly Column<string> vc_user_group_name = Column<string>.Text();

        public readonly Column<string[]?> vc_group_members = Column<string[]?>.Text(size: 0, fake: true, isArray: true, nullable: true);

        public readonly ViewDefinition mug_brwStandard = new(nameof(c_user_group_id));

        public readonly ProcedureDefinition mug_GetAllGroupsAllowedRoutes = new(readonly_locks: true);

        public readonly EntityUniqueConstraint UNGroupName = new(keys: [nameof(vc_user_group_name)]);

        public readonly EntityForeignKey<MicromUsers, MicromUsersGroups> FKUsers = new(fake: true);

    }

    public class MicromUsersGroups : Entity<MicromUsersGroupsDef>
    {
        public MicromUsersGroups() : base() { }
        public MicromUsersGroups(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

        public override async Task<DBStatusResult> InsertData(CancellationToken ct, bool throw_dbstat_exception = false, MicroMOptions? options = null, Dictionary<string, object>? claims = null, IMicroMWebAPI? api = null, string? app_id = null)
        {
            var result = await base.InsertData(ct, throw_dbstat_exception, options, claims, api, app_id);

            if (api != null && !string.IsNullOrEmpty(app_id)) await api.SecurityService.RefreshGroupsSecurityRecords(app_id, ct);

            return result;
        }

        public override async Task<DBStatusResult> UpdateData(CancellationToken ct, bool throw_dbstat_exception = false, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IMicroMWebAPI? api = null, string? app_id = null)
        {
            var result = await base.UpdateData(ct, throw_dbstat_exception, options, server_claims, api, app_id);

            if (api != null && !string.IsNullOrEmpty(app_id)) await api.SecurityService.RefreshGroupsSecurityRecords(app_id, ct);

            return result;
        }

        public override async Task<DBStatusResult> DeleteData(CancellationToken ct, bool throw_dbstat_exception = false, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IMicroMWebAPI? api = null, string? app_id = null)
        {
            var result = await base.DeleteData(ct, throw_dbstat_exception, options, server_claims, api);

            if (api != null && !string.IsNullOrEmpty(app_id)) await api.SecurityService.RefreshGroupsSecurityRecords(app_id, ct);

            return result;
        }

    }


}
