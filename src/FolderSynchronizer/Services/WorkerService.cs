using FolderSynchronizer.Interfaces;
using FolderSynchronizer.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FolderSynchronizer.Services;

public class WorkerService(IOptions<WorkerOptions> options, IFileSynchronizationService fileSynchronization, ILogWriter logWriter) : BackgroundService
{
    private readonly int _syncInterval = options.Value.SyncInterval;
    private readonly string _copyFromPath = options.Value.CopyFromPath;
    private readonly string _copyToPath = options.Value.CopyToPath;

    /// <summary>
    /// A background service that periodically checks for directories and synchronizes files.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            CheckForDirectories();
            await fileSynchronization.SynchronizeFilesAsync(cancellationToken);
            await Task.Delay(_syncInterval, cancellationToken);
        }

        TerminateApplication(0);
    }

    /// <summary>
    /// Checks if the source and replica directories exist and perform the appropriate action.
    /// </summary>
    public void CheckForDirectories()
    {
        // If source directory does not exist, the program has nothing to do
        if (!Directory.Exists(_copyFromPath))
        {
            logWriter.LogAsync($"Source directory '{_copyFromPath}' does not exist");
            TerminateApplication(-1);
        }
        // If replica directory does not exist, create it and log the action
        if (!Directory.Exists(_copyToPath))
        {
            logWriter.LogAsync($"Created replica directory '{_copyToPath}'");
            Directory.CreateDirectory(_copyToPath);
        }
    }

    /// <summary>
    /// A dedicated method to facilitate application testing
    /// </summary>
    protected virtual void TerminateApplication(int exitCode)
    {
        logWriter.LogAsync($"Application terminated with exit code {exitCode}");
        Environment.Exit(exitCode);
    }
}