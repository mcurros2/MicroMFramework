using MicroM.Configuration;
using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;
using System.Data;
using static MicroM.DataDictionary.ConfigurationDBHandlers;
using static System.ArgumentNullException;

namespace MicroM.DataDictionary;

/// <summary>
/// Definition of configuration database parameters stored for server setup.
/// </summary>
public class ConfigurationDBDef : EntityDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationDBDef"/> class.
    /// </summary>
    public ConfigurationDBDef() : base("dfg", nameof(ConfigurationDB)) { Fake = true; }

    /// <summary>
    /// Primary identifier for the configuration record.
    /// </summary>
    public readonly Column<string> c_confgidb_id = Column<string>.PK(value: "1");

    /// <summary>
    /// SQL server hosting the configuration database.
    /// </summary>
    public readonly Column<string> vc_configsqlserver = new(sql_type: SqlDbType.VarChar, size: 255);

    /// <summary>
    /// SQL login used for configuration operations.
    /// </summary>
    public readonly Column<string> vc_configsqluser = new(sql_type: SqlDbType.VarChar, size: 255);

    /// <summary>
    /// Password for the configuration SQL login.
    /// </summary>
    public readonly Column<string?> vc_configsqlpassword = new(sql_type: SqlDbType.VarChar, size: 2048, nullable: true);

    /// <summary>
    /// Name of the configuration database.
    /// </summary>
    public readonly Column<string> vc_configdatabase = new(sql_type: SqlDbType.VarChar, size: 255);

    /// <summary>
    /// Thumbprint of the certificate used for encryption.
    /// </summary>
    public readonly Column<string> vc_certificatethumbprint = new(sql_type: SqlDbType.VarChar, size: 255);

    /// <summary>
    /// Password protecting the certificate's private key.
    /// </summary>
    public readonly Column<string> vc_certificatepassword = new(sql_type: SqlDbType.VarChar, size: 2048);

    /// <summary>
    /// Display name of the certificate.
    /// </summary>
    public readonly Column<string> vc_certificatename = new(sql_type: SqlDbType.VarChar, size: 2048);

    /// <summary>
    /// Indicates whether the administrator has required rights.
    /// </summary>
    public readonly Column<bool> b_adminuserhasrights = new(sql_type: SqlDbType.Bit);

    /// <summary>
    /// Flag indicating if the configuration database exists.
    /// </summary>
    public readonly Column<bool> b_configdbexists = new(sql_type: SqlDbType.Bit);

    /// <summary>
    /// Flag indicating if the configuration user exists.
    /// </summary>
    public readonly Column<bool> b_configuserexists = new(sql_type: SqlDbType.Bit);

    /// <summary>
    /// Flag indicating if secrets have been configured.
    /// </summary>
    public readonly Column<bool> b_secretsconfigured = new(sql_type: SqlDbType.Bit);

    /// <summary>
    /// Flag indicating if the default certificate was used.
    /// </summary>
    public readonly Column<bool> b_defaultcertificate = new(sql_type: SqlDbType.Bit);

    /// <summary>
    /// Flag indicating if a certificate thumbprint is configured.
    /// </summary>
    public readonly Column<bool> b_thumbprintconfigured = new(sql_type: SqlDbType.Bit);

    /// <summary>
    /// Flag indicating if the configured thumbprint was found.
    /// </summary>
    public readonly Column<bool> b_thumbprintfound = new(sql_type: SqlDbType.Bit);

    /// <summary>
    /// Flag indicating if a certificate was found.
    /// </summary>
    public readonly Column<bool> b_certificatefound = new(sql_type: SqlDbType.Bit);

    /// <summary>
    /// Flag indicating if the secrets file is valid.
    /// </summary>
    public readonly Column<bool> b_secretsfilevalid = new(sql_type: SqlDbType.Bit);

    /// <summary>
    /// Indicates whether the database should be recreated.
    /// </summary>
    public readonly Column<bool> b_recreatedatabase = new(sql_type: SqlDbType.Bit);

    /// <summary>
    /// Default browse view definition.
    /// </summary>
    public ViewDefinition dfg_brwStandard { get; private set; } = new(nameof(c_confgidb_id));

}

/// <summary>
/// Entity used to manage configuration database settings.
/// </summary>
public class ConfigurationDB : Entity<ConfigurationDBDef>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationDB"/> class.
    /// </summary>
    public ConfigurationDB() : base() { }

    /// <summary>
    /// Initializes a new instance with a database client and optional encryptor.
    /// </summary>
    /// <param name="ec">Database client.</param>
    /// <param name="encryptor">Optional encryptor.</param>
    public ConfigurationDB(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }


    /// <summary>
    /// Retrieves configuration data.
    /// </summary>
    /// <inheritdoc/>
    public override async Task<bool> GetData(CancellationToken ct, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IWebAPIServices? api = null, string? app_id = null)
    {
        ThrowIfNull(server_claims);
        ThrowIfNull(options);
        return await HandleGetData(this, options, server_claims, ct);
    }

    /// <summary>
    /// Updates configuration data.
    /// </summary>
    /// <inheritdoc/>
    public override async Task<DBStatusResult> UpdateData(CancellationToken ct, bool throw_dbstat_exception = false, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IWebAPIServices? api = null, string? app_id = null)
    {
        ThrowIfNull(server_claims);
        ThrowIfNull(options);
        return await HandleUpdateData(this, throw_dbstat_exception, options, server_claims, api, ct);
    }
}
