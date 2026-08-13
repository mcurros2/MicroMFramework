namespace MicroM.Core;

public static class FilesProvider
{
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
