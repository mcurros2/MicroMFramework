using MicroM.Configuration;
using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;
using System.Data;
using static MicroM.DataDictionary.ConfigurationDBHandlers;
using static System.ArgumentNullException;

namespace MicroM.DataDictionary;

/// <summary>
/// Schema definition for configuration database settings.
/// </summary>
public class ConfigurationDBDef : EntityDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationDBDef"/> class.
    /// </summary>
    public ConfigurationDBDef() : base("dfg", nameof(ConfigurationDB)) { Fake = true; }

    /// <summary>
    /// Fixed identifier for configuration database records.
    /// </summary>
    public readonly Column<string> c_confgidb_id = Column<string>.PK(value: "1");

    /// <summary>
    /// SQL Server host name.
    /// </summary>
    public readonly Column<string> vc_configsqlserver = new(sql_type: SqlDbType.VarChar, size: 255);
    /// <summary>
    /// SQL Server user name.
    /// </summary>
    public readonly Column<string> vc_configsqluser = new(sql_type: SqlDbType.VarChar, size: 255);
    /// <summary>
    /// SQL Server password.
    /// </summary>
    public readonly Column<string?> vc_configsqlpassword = new(sql_type: SqlDbType.VarChar, size: 2048, nullable: true);
    /// <summary>
    /// Configuration database name.
    /// </summary>
    public readonly Column<string> vc_configdatabase = new(sql_type: SqlDbType.VarChar, size: 255);

    /// <summary>
    /// Certificate thumbprint used for encryption.
    /// </summary>
    public readonly Column<string> vc_certificatethumbprint = new(sql_type: SqlDbType.VarChar, size: 255);
    /// <summary>
    /// Password for the certificate.
    /// </summary>
    public readonly Column<string> vc_certificatepassword = new(sql_type: SqlDbType.VarChar, size: 2048);
    /// <summary>
    /// File name of the certificate.
    /// </summary>
    public readonly Column<string> vc_certificatename = new(sql_type: SqlDbType.VarChar, size: 2048);

    /// <summary>
    /// Indicates whether the admin user has rights.
    /// </summary>
    public readonly Column<bool> b_adminuserhasrights = new(sql_type: SqlDbType.Bit);
    /// <summary>
    /// Indicates whether the configuration database exists.
    /// </summary>
    public readonly Column<bool> b_configdbexists = new(sql_type: SqlDbType.Bit);
    /// <summary>
    /// Indicates whether the configuration user exists.
    /// </summary>
    public readonly Column<bool> b_configuserexists = new(sql_type: SqlDbType.Bit);
    /// <summary>
    /// Indicates whether secrets have been configured.
    /// </summary>
    public readonly Column<bool> b_secretsconfigured = new(sql_type: SqlDbType.Bit);
    /// <summary>
    /// Indicates whether a default certificate is being used.
    /// </summary>
    public readonly Column<bool> b_defaultcertificate = new(sql_type: SqlDbType.Bit);
    /// <summary>
    /// Indicates whether a thumbprint is configured.
    /// </summary>
    public readonly Column<bool> b_thumbprintconfigured = new(sql_type: SqlDbType.Bit);
    /// <summary>
    /// Indicates whether the configured thumbprint is found.
    /// </summary>
    public readonly Column<bool> b_thumbprintfound = new(sql_type: SqlDbType.Bit);
    /// <summary>
    /// Indicates whether the certificate file is found.
    /// </summary>
    public readonly Column<bool> b_certificatefound = new(sql_type: SqlDbType.Bit);
    /// <summary>
    /// Indicates whether the secrets file is valid.
    /// </summary>
    public readonly Column<bool> b_secretsfilevalid = new(sql_type: SqlDbType.Bit);

    /// <summary>
    /// Indicates whether the database should be recreated.
    /// </summary>
    public readonly Column<bool> b_recreatedatabase = new(sql_type: SqlDbType.Bit);

    /// <summary>
    /// Standard browse view for configuration database records.
    /// </summary>
    public ViewDefinition dfg_brwStandard { get; private set; } = new(nameof(c_confgidb_id));

}

/// <summary>
/// Runtime entity for accessing and validating configuration database settings.
/// </summary>
public class ConfigurationDB : Entity<ConfigurationDBDef>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationDB"/> class.
    /// </summary>
    public ConfigurationDB() : base() { }
    /// <summary>
    /// Initializes a new instance using the specified entity client and optional encryptor.
    /// </summary>
    /// <param name="ec">Entity client used for data access.</param>
    /// <param name="encryptor">Optional encryptor for sensitive fields.</param>
    public ConfigurationDB(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }


    /// <summary>
    /// Retrieves configuration data using the provided options and claims.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <param name="options">Framework configuration options.</param>
    /// <param name="server_claims">Server claims containing connection information.</param>
    /// <param name="api">Optional Web API services instance.</param>
    /// <param name="app_id">Optional application identifier.</param>
    /// <returns>True if data retrieval succeeded; otherwise, false.</returns>
    public override async Task<bool> GetData(CancellationToken ct, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IWebAPIServices? api = null, string? app_id = null)
    {
        ThrowIfNull(server_claims);
        ThrowIfNull(options);
        return await HandleGetData(this, options, server_claims, ct);
    }

    /// <summary>
    /// Updates configuration data using the provided options and claims.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <param name="throw_dbstat_exception">Whether to throw if the database status indicates an error.</param>
    /// <param name="options">Framework configuration options.</param>
    /// <param name="server_claims">Server claims containing connection information.</param>
    /// <param name="api">Optional Web API services instance.</param>
    /// <param name="app_id">Optional application identifier.</param>
    /// <returns>Result of the database operation.</returns>
    public override async Task<DBStatusResult> UpdateData(CancellationToken ct, bool throw_dbstat_exception = false, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IWebAPIServices? api = null, string? app_id = null)
    {
        ThrowIfNull(server_claims);
        ThrowIfNull(options);
        return await HandleUpdateData(this, throw_dbstat_exception, options, server_claims, api, ct);
    }
}
