using FolderSynchronizer.Interfaces;
using FolderSynchronizer.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FolderSynchronizer.Services;

public class WorkerService(IOptions<WorkerOptions> options, IFileSynchronizationService fileSynchronization, ILogWriter logWriter) : BackgroundService
{
    // Injecting WorkerOptions to get configuration values from appsettings.json
    private readonly int _syncInterval = options.Value.SyncInterval;
    private readonly string _logDirectory = logWriter.GetLogDirectory();
    private readonly string _copyFromPath = options.Value.CopyFromPath;
    private readonly string _copyToPath = options.Value.CopyToPath;

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            // Ensure the log directory exists
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
                await logWriter.LogAsync($"Created log directory '{_logDirectory}' with file {logWriter.GetLogFileName()}");
            }

            CheckForDirectories();
            await fileSynchronization.SynchronizeFilesAsync(cancellationToken);

            await Task.Delay(_syncInterval, cancellationToken);
        }

        TerminateApplication(0);
    }

    /// <summary>
    /// Checks if the source and replica directories exist, creates them if they do not, and logs the actions.
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

    protected virtual void TerminateApplication(int exitCode)
    {
        logWriter.LogAsync($"Application terminated with exit code {exitCode}");
        Environment.Exit(exitCode);
    }
}