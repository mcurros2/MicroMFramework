using MicroM.Configuration;
using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary;

/// <summary>
/// Schema definition for user groups.
/// </summary>
public class MicromUsersGroupsDef : EntityDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MicromUsersGroupsDef"/> class.
    /// </summary>
    public MicromUsersGroupsDef() : base("mug", nameof(MicromUsersGroups)) { SQLCreationOptions = SQLCreationOptionsMetadata.WithIUpdate; }

    /// <summary>Group identifier.</summary>
    public readonly Column<string> c_user_group_id = Column<string>.PK(autonum: true);
    /// <summary>Descriptive name of the group.</summary>
    public readonly Column<string> vc_user_group_name = Column<string>.Text();

    /// <summary>Identifiers of members belonging to the group.</summary>
    public readonly Column<string[]?> vc_group_members = Column<string[]?>.Text(size: 0, fake: true, isArray: true, nullable: true);

    /// <summary>Default browse view definition.</summary>
    public readonly ViewDefinition mug_brwStandard = new(nameof(c_user_group_id));

    /// <summary>Procedure to retrieve allowed routes for all groups.</summary>
    public readonly ProcedureDefinition mug_GetAllGroupsAllowedRoutes = new(readonly_locks: true);

    /// <summary>Unique constraint enforcing group name uniqueness.</summary>
    public readonly EntityUniqueConstraint UNGroupName = new(keys: [nameof(vc_user_group_name)]);

    /// <summary>Relationship to users.</summary>
    public readonly EntityForeignKey<MicromUsers, MicromUsersGroups> FKUsers = new(fake: true);

}

/// <summary>
/// Entity for managing user group records.
/// </summary>
public class MicromUsersGroups : Entity<MicromUsersGroupsDef>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MicromUsersGroups"/> class.
    /// </summary>
    public MicromUsersGroups() : base() { }
    /// <summary>
    /// Initializes a new instance with a database client and optional encryptor.
    /// </summary>
    /// <param name="ec">Entity client.</param>
    /// <param name="encryptor">Optional encryptor.</param>
    public MicromUsersGroups(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

    /// <summary>
    /// Inserts group data and refreshes related security records.
    /// </summary>
    public override async Task<DBStatusResult> InsertData(CancellationToken ct, bool throw_dbstat_exception = false, MicroMOptions? options = null, Dictionary<string, object>? claims = null, IWebAPIServices? api = null, string? app_id = null)
    {
        var result = await base.InsertData(ct, throw_dbstat_exception, options, claims, api, app_id);

        if (api != null && !string.IsNullOrEmpty(app_id)) await api.securityService.RefreshGroupsSecurityRecords(app_id, ct);

        return result;
    }

    /// <summary>
    /// Updates group data and refreshes related security records.
    /// </summary>
    public override async Task<DBStatusResult> UpdateData(CancellationToken ct, bool throw_dbstat_exception = false, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IWebAPIServices? api = null, string? app_id = null)
    {
        var result = await base.UpdateData(ct, throw_dbstat_exception, options, server_claims, api, app_id);

        if (api != null && !string.IsNullOrEmpty(app_id)) await api.securityService.RefreshGroupsSecurityRecords(app_id, ct);

        return result;
    }

    /// <summary>
    /// Deletes group data and refreshes related security records.
    /// </summary>
    public override async Task<DBStatusResult> DeleteData(CancellationToken ct, bool throw_dbstat_exception = false, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IWebAPIServices? api = null, string? app_id = null)
    {
        var result = await base.DeleteData(ct, throw_dbstat_exception, options, server_claims, api);

        if (api != null && !string.IsNullOrEmpty(app_id)) await api.securityService.RefreshGroupsSecurityRecords(app_id, ct);

        return result;
    }

}

