namespace FolderSynchronizer.Options;

public class WorkerOptions
{
    public int SyncInterval { get; set; }
    public string LogFileFullPath { get; set; }
    public string CopyFromPath { get; set; }
    public string CopyToPath { get; set; }
}