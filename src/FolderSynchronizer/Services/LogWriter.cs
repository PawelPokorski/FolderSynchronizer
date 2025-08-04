using FolderSynchronizer.Interfaces;
using FolderSynchronizer.Options;
using Microsoft.Extensions.Options;

namespace FolderSynchronizer.Services;

public class LogWriter(IOptions<WorkerOptions> options) : ILogWriter
{
    private readonly string _logFilePath = options.Value.LogFileFullPath;

    public async Task LogAsync(string message)
    {
        var directory = Path.GetDirectoryName(_logFilePath);

        // Ensure the log directory exists
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var logWriter = new StreamWriter(_logFilePath, true);
        await logWriter.WriteLineAsync($"[{DateTime.Now}] {message}");
    }

    public string GetLogFileName()
    {
        return Path.GetFileName(_logFilePath);
    }

    public string GetLogDirectory()
    {
        return Path.GetDirectoryName(_logFilePath) ?? string.Empty;
    }
}