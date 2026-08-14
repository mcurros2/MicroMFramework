namespace MicroM.Core;

public static class FilesProvider
{
    public static string GetFilePath(string uploadsFolder, string appId, string fileFolder, string fileGuid)
    {
        var uploadsPath = Path.GetFullPath(Path.Combine(uploadsFolder, appId, fileFolder));
        var filePath = Path.GetFullPath(Path.Combine(uploadsPath, fileGuid));

        if (!filePath.StartsWith(uploadsPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"File GUID '{fileGuid}' resolves outside the upload directory.");
        }

        return filePath;
    }

    public static bool TryDeleteFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return true;

        try
        {
            File.Delete(path);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static FileStream OpenSequentialWriteStream(string path, int writeBufferSize)
    {
        return new FileStream(
            path,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            writeBufferSize,
            FileOptions.Asynchronous | FileOptions.SequentialScan
            );
    }

    public static FileStream? TryOpenSequentialReadStream(string path, int readBufferSize)
    {
        try
        {
            return new FileStream(
                path,
                FileMode.Open, FileAccess.Read, FileShare.Read,
                readBufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan
                );
        }
        catch
        {
            return null;
        }

    }
}
