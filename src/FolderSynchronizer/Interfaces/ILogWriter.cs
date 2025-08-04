namespace FolderSynchronizer.Interfaces;

public interface ILogWriter
{
    Task LogAsync(string message);
    string GetLogFileName();
    string GetLogDirectory();
}