using FolderSynchronizer.Interfaces;
using FolderSynchronizer.Options;
using Microsoft.Extensions.Options;

namespace FolderSynchronizer.Services;

public class LogWriter(IOptions<WorkerOptions> options) : ILogWriter
{
    private readonly string _logFilePath = options.Value.LogFileFullPath;

    /// <summary>
    /// Asynchronously logs a message to the log file.
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public async Task LogAsync(string message)
    {
        // Ensure the log directory exists
        if (!Directory.Exists(GetLogDirectory()))
        {
            Directory.CreateDirectory(GetLogDirectory());
            await LogAsync($"Created log directory '{GetLogDirectory()}' with file {GetLogFileName()}");
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