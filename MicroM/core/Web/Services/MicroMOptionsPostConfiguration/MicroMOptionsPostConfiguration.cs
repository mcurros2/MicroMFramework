using MicroM.Configuration;
using Microsoft.Extensions.Options;

namespace MicroM.Web.Services;

public class MicroMOptionsPostConfiguration : IPostConfigureOptions<MicroMOptions>
{
    public void PostConfigure(string? name, MicroMOptions options)
    {
        options.ConfigSQLServerDB ??= ConfigurationDefaults.SQLConfigDatabaseName;
        options.AllowedUploadFileExtensions ??= ConfigurationDefaults.AllowedFileUploadExtensions;

        // MMC: change defaults to the values provided in MicroMOptions if they are not -1 (default values are used deep in the framework where options are not injected)
        if (options.DefaultConnectionTimeOutInSecs != -1) DataDefaults.DefaultConnectionTimeOutInSecs = options.DefaultConnectionTimeOutInSecs;
        if (options.DefaultRowLimitForViews != -1) DataDefaults.DefaultRowLimitForViews = options.DefaultRowLimitForViews;
        if (options.DefaultCommandTimeOutInMins != -1) DataDefaults.DefaultCommandTimeOutInMins = options.DefaultCommandTimeOutInMins;

        options.MicroMBaseFolderPath ??= ConfigurationDefaults.MicroMConfigurationFolder;
        options.UploadsFolder ??= Path.Combine(options.MicroMBaseFolderPath!, options.ConfigSQLServerDB!, "uploads");
        options.SecretsFilePath ??= options.MicroMBaseFolderPath;

        options.DiskFileCacheOptions ??= new DiskFileCacheOptions();

        if (string.IsNullOrEmpty(options.DiskFileCacheOptions.RootPath))
        {
            options.DiskFileCacheOptions.RootPath = Path.Combine(options.MicroMBaseFolderPath!, options.ConfigSQLServerDB!, "disk_cache");
        }

        options.EntitiesDLLStoragePath ??= Path.Combine(options.MicroMBaseFolderPath, nameof(MicroMOptions.MicroM), "microm_entities_dlls");
        options.SecretsFileName ??= ConfigurationDefaults.SecretsFilename;
    }
}
