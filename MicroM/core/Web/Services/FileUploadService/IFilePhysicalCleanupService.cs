namespace MicroM.Web.Services;

public interface IFilePhysicalCleanupService
{
    bool TryCleanup(string appId, FileDetails fileDetails);
}
