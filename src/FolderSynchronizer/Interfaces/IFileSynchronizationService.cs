namespace FolderSynchronizer.Interfaces;

public interface IFileSynchronizationService
{
    Task SynchronizeFilesAsync(CancellationToken cancellationToken);
    Task CheckAndCopyFiles(CancellationToken cancellationToken);
    Task CleanupFiles(CancellationToken cancellationToken);

    Task<bool> CompareFiles(string filePath1, string filePath2, CancellationToken cancellationToken);
    Task<bool> CheckIfCopyFileNeeded(string path, string sourceFile, CancellationToken cancellationToken);
}