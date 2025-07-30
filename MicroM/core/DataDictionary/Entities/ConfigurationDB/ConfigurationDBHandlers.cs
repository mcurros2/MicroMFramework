using MicroM.Configuration;
using MicroM.Core;
using MicroM.Data;
using MicroM.Extensions;
using MicroM.Web.Authentication;
using MicroM.Web.Services;
using System.Security.Cryptography;
using static MicroM.Database.ConfigurationDatabaseSchema;
using static MicroM.Database.DatabaseManagement;
using static MicroM.Database.DatabaseSchemaPermissions;
using static MicroM.Validators.Expressions;


namespace MicroM.DataDictionary;

public static class ConfigurationDBHandlers
{

    private async static Task<InitialConfigurationResult> CheckInitialStatus(IEntityClient dbc, string config_user, string configuration_db, CancellationToken ct)
    {
        InitialConfigurationResult result = new()
        {
            AdminUserHasRights = await LoggedInUserHasAdminRights(dbc, ct)
        };
        if (result.AdminUserHasRights)
        {
            result.ConfigUserExists = await UserExists(dbc, config_user, ct);
            result.ConfigDBExists = await DatabaseExists(dbc, configuration_db, ct);
        }
        return result;
    }

    private async static Task SaveConfigurationDBParms(SecretsOptions options, string certificate_thumbprint, CancellationToken ct)
    {
        using var cert = CryptClass.FindCertificate(certificate_thumbprint) ?? throw new ArgumentException($"Certificate not found {certificate_thumbprint}");

        var encrypted = CryptClass.EncryptObject<SecretsOptions>(options, cert);

        string config_path = Path.Combine(ConfigurationDefaults.SecretsFilePath, ConfigurationDefaults.MicroMCommonID);

        if (!Directory.Exists(config_path))
        {
            Directory.CreateDirectory(config_path);
        }

        string config_file = Path.Combine(config_path, ConfigurationDefaults.SecretsFilename);

        await File.WriteAllTextAsync(config_file, encrypted, ct);
    }

    public async static Task<SecretsOptions?> ReadConfigurationDBParms(string certificate_thumbprint, CancellationToken ct)
    {
        SecretsOptions? result = null;

        using var cert = CryptClass.FindCertificate(certificate_thumbprint);
        if (cert != null)
        {
            string config_path = Path.Combine(ConfigurationDefaults.SecretsFilePath, ConfigurationDefaults.MicroMCommonID, ConfigurationDefaults.SecretsFilename);
            if (File.Exists(config_path))
            {
                string encrypted = await File.ReadAllTextAsync(config_path, ct);
                result = CryptClass.DecryptObject<SecretsOptions>(encrypted, cert);
            }
        }

        return result;
    }

    public async static Task<bool> HandleGetData(ConfigurationDB cfg, MicroMOptions options, Dictionary<string, object> server_claims, CancellationToken ct)
    {
        // MMC: this is the logged in user to the control panel
        server_claims.TryGetValue(MicroMServerClaimTypes.MicroMUsername, out var admin_user_obj);
        server_claims.TryGetValue(MicroMServerClaimTypes.MicroMPassword, out var admin_password_obj);

        string? admin_user = (string?)admin_user_obj;
        string? admin_password = (string?)admin_password_obj;

        if (string.IsNullOrEmpty(admin_user)) throw new ArgumentNullException(nameof(server_claims));

        string? thumbprint = options.CertificateThumbprint;

        // MMC: try to find the configured certificate first
        bool found = false;
        if (!string.IsNullOrEmpty(options.CertificateThumbprint))
        {
            using var cert = CryptClass.FindCertificate(options.CertificateThumbprint);
            cfg.Def.b_thumbprintconfigured.Value = true;
            found = cert != null;
            cfg.Def.b_thumbprintfound.Value = found;
            if (cert != null) cfg.Def.vc_certificatename.Value = cert.SubjectName.Name;
        }

        // MMC: no thumbprint configured or found, try default certificate name
        if (!found)
        {
            using var cert = CryptClass.FindCertificateByName(ConfigurationDefaults.CertificateSubjectName);
            thumbprint = cert?.Thumbprint;
            cfg.Def.b_defaultcertificate.Value = cert != null;
            found = cert != null;
            if (cert != null) cfg.Def.vc_certificatename.Value = cert.SubjectName.Name;
        }

        cfg.Def.b_certificatefound.Value = found;

        // MMC: a certificate was found, try to get sql configuration user from secrets
        SecretsOptions? secrets = null;
        if (found && !string.IsNullOrEmpty(thumbprint))
        {
            try
            {
                secrets = await ReadConfigurationDBParms(thumbprint, ct);
                cfg.Def.b_secretsconfigured.Value = secrets != null;
                cfg.Def.b_secretsfilevalid.Value = true;
            }
            catch (CryptographicException)
            {
                cfg.Def.b_secretsfilevalid.Value = false;
            }
        }

        var config_user = secrets?.ConfigSQLUser ?? cfg.Def.vc_configsqluser.Value ?? ConfigurationDefaults.SQLConfigUser;
        var config_db = options.ConfigSQLServerDB ?? cfg.Def.vc_configdatabase.Value ?? ConfigurationDefaults.SQLConfigDatabaseName;

        InitialConfigurationResult checks = new();
        if (!string.IsNullOrEmpty(options.ConfigSQLServer) && !string.IsNullOrEmpty(config_user))
        {

            using IEntityClient dbc = cfg.Client.Clone(options.ConfigSQLServer, cfg.Client.MasterDatabase, admin_user, admin_password ?? "");
            await dbc.Connect(ct);
            checks = await CheckInitialStatus(dbc, config_user, config_db, ct);
        }

        cfg.Def.vc_certificatethumbprint.Value = thumbprint!;
        cfg.Def.vc_configsqlserver.Value = options.ConfigSQLServer!;
        cfg.Def.vc_configdatabase.Value = config_db;
        cfg.Def.vc_configsqluser.Value = config_user;
        cfg.Def.vc_configsqlpassword.Value = secrets?.ConfigSQLPassword;
        cfg.Def.b_adminuserhasrights.Value = checks.AdminUserHasRights;
        cfg.Def.b_configdbexists.Value = checks.ConfigDBExists;
        cfg.Def.b_configuserexists.Value = checks.ConfigUserExists;

        return true;

    }

    public async static Task<DBStatusResult> HandleUpdateData(ConfigurationDB cfg, bool throw_dbstat_exception, MicroMOptions options, Dictionary<string, object> server_claims, IWebAPIServices? api, CancellationToken ct)
    {
        // MMC: this is the logged in user to the control panel
        server_claims.TryGetValue(MicroMServerClaimTypes.MicroMUsername, out var admin_user_obj);
        server_claims.TryGetValue(MicroMServerClaimTypes.MicroMPassword, out var admin_password_obj);

        string? admin_user = (string?)admin_user_obj;
        string? admin_password = (string?)admin_password_obj;

        if (string.IsNullOrEmpty(admin_user)) throw new ArgumentNullException(nameof(server_claims));

        List<DBStatus> errors = [];

        if (!OnlyDigitNumbersAndUnderscore().IsMatch(cfg.Def.vc_configdatabase.Value))
        {
            errors.Add(new() { Status = DBStatusCodes.Error, Message = "Configuration Database Name is invalid" });
        }

        if (!ValidSQLServerLogin().IsMatch(cfg.Def.vc_configsqluser.Value))
        {
            errors.Add(new() { Status = DBStatusCodes.Error, Message = "Configuration SQL Username is invalid" });
        }

        if (string.IsNullOrEmpty(options.ConfigSQLServer))
        {
            errors.Add(new() { Status = DBStatusCodes.Error, Message = $"{nameof(options.ConfigSQLServer)} not specified in appsettings file" });
        }

        if (errors.Count > 0)
        {
            return new() { Failed = true, Results = errors };
        }


        // MMC: check existing config first
        ConfigurationDB existing_config = cfg.Clone(false);

        await HandleGetData(existing_config, options, server_claims, ct);

        // No admin rigths
        if (existing_config.Def.b_adminuserhasrights.Value == false)
        {
            return new() { Failed = true, Results = [new() { Status = DBStatusCodes.Error, Message = "The logged in user has no admin user rights" }] };
        }

        // If we have logged in, a certificate will always be created, so don't create one here, as the same failure will exist
        if (existing_config.Def.b_certificatefound.Value == false)
        {
            return new() { Failed = true, Results = [new() { Status = DBStatusCodes.Error, Message = "A certificate was not found. Check if the service and user have permissions to create a certificate in the user store." }] };
        }

        // Config user exists, but the config DB don't, this can lead to a configuration password mismatch
        if (existing_config.Def.b_secretsfilevalid.Value == false)
        {
            return new() { Failed = true, Results = [new() { Status = DBStatusCodes.Error, Message = "A secrets file exists but can't be decrypted. Delete the file if you are trying to reconfigure the server from scratch." }] };
        }

        // Config user exists, but the config DB don't, this can lead to a configuration password mismatch
        if (existing_config.Def.b_configuserexists.Value && existing_config.Def.b_configdbexists.Value == false && cfg.Def.b_recreatedatabase.Value == false)
        {
            return new() { Failed = true, Results = [new() { Status = DBStatusCodes.Error, Message = "The configuration user exists, but the config database don't. Please delete the existing login to reconfigure the database." }] };
        }

        // MMC: a certificate was found, try to get sql configuration user from secrets and ignore the configuration user specified
        SecretsOptions? secrets = null;
        if (existing_config.Def.b_secretsconfigured.Value && existing_config.Def.b_configdbexists.Value)
        {
            secrets = await ReadConfigurationDBParms(existing_config.Def.vc_certificatethumbprint.Value, ct);
            if (secrets?.ConfigSQLUser != cfg.Def.vc_configsqluser.Value)
            {
                return new() { Failed = true, Results = [new() { Status = DBStatusCodes.Error, Message = "The specified configuration username is different from the secrets configuration. Delete the existing configuration." }] };
            }
            if (!string.IsNullOrEmpty(secrets?.ConfigSQLUser)) cfg.Def.vc_configsqluser.Value = secrets.ConfigSQLUser;
        }

        // MMC: create uploads folder
        string uploads_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, options.UploadsFolder ?? ConfigurationDefaults.UploadsFolder);
        if (!Path.Exists(uploads_path)) Directory.CreateDirectory(uploads_path);

        using IEntityClient dbc = cfg.Client.Clone(options.ConfigSQLServer ?? "", cfg.Client.MasterDatabase, admin_user, admin_password ?? "");

        try
        {

            await dbc.Connect(ct);

            string? new_password = CryptClass.CreateRandomPassword();

            // MMC: recreate the database if specified
            if (cfg.Def.b_recreatedatabase.Value)
            {
                await dbc.ExecuteSQLNonQuery($"begin try\nalter database [{cfg.Def.vc_configdatabase.Value}] set single_user with rollback immediate\nend try\nbegin catch\nend catch", ct);
                await dbc.ExecuteSQLNonQuery($"drop database if exists [{cfg.Def.vc_configdatabase.Value}]", ct);
                await dbc.ExecuteSQLNonQuery($"begin try\ndrop login [{cfg.Def.vc_configsqluser.Value}]\nend try\nbegin catch\nend catch", ct);
                existing_config.Def.b_configdbexists.Value = false;
            }

            if (!existing_config.Def.b_configdbexists.Value)
            {
                await dbc.ExecuteSQLNonQuery($"create database [{cfg.Def.vc_configdatabase.Value}]", ct);
                await dbc.ExecuteSQLNonQuery($"alter database [{cfg.Def.vc_configdatabase.Value}] set recovery simple", ct);

                await dbc.ExecuteSQLNonQuery($"use [{cfg.Def.vc_configdatabase.Value}]", ct);

                await dbc.ExecuteSQLNonQuery($"create login [{cfg.Def.vc_configsqluser.Value}] with password = '{new_password}', check_expiration = off, check_policy = off, default_database = [{cfg.Def.vc_configdatabase.Value}]", ct);
                await dbc.ExecuteSQLNonQuery($"if user_id('{cfg.Def.vc_configsqluser.Value}') is not null drop user [{cfg.Def.vc_configsqluser.Value}]", ct);
                await dbc.ExecuteSQLNonQuery($"create user [{cfg.Def.vc_configsqluser.Value}] with default_schema = [dbo]", ct);
            }
            else
            {
                if (existing_config.Def.b_configuserexists.Value) await dbc.ExecuteSQLNonQuery($"alter login [{cfg.Def.vc_configsqluser.Value}] with password = '{new_password}'", ct);
            }

            await dbc.ExecuteSQLNonQuery($"use [{cfg.Def.vc_configdatabase.Value}]", ct);

            var entities = GetConfigurationEntitiesTypes(dbc);
            await CreateConfigurationDBSchemaAndProcs(dbc, entities, ct, true);
            await GrantExecutionToAllProcs(dbc, entities, cfg.Def.vc_configsqluser.Value, ct);

            secrets = new() { ConfigSQLUser = cfg.Def.vc_configsqluser.Value, ConfigSQLPassword = new_password };
            await SaveConfigurationDBParms(secrets, existing_config.Def.vc_certificatethumbprint.Value, ct);
            if (api != null)
            {
                await api.app_config.RefreshConfiguration(null, ct);
                await api.securityService.RefreshGroupsSecurityRecords(null, ct);
            }

            return new() { Results = [new() { Status = DBStatusCodes.OK }] };
        }
        finally
        {
            await dbc.Disconnect();
        }

    }


}
