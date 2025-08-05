using FolderSynchronizer.Interfaces;
using FolderSynchronizer.Options;
using Microsoft.Extensions.Options;

namespace FolderSynchronizer.Services;

public class FileSynchronizationService(IOptions<WorkerOptions> options, ILogWriter logWriter) : IFileSynchronizationService
{
    private readonly string _copyFromPath = options.Value.CopyFromPath;
    private readonly string _copyToPath = options.Value.CopyToPath;

    /// <summary>
    /// Synchronizes files from the source directory to the replica directory.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task SynchronizeFilesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await CheckAndCopyFiles(cancellationToken);
            await CleanupFiles(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            await logWriter.LogAsync($"Synchronization cancelled");
        }
    }

    /// <summary>
    /// Check the source directory and copy files to the replica directory if they do not exist or are different.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task CheckAndCopyFiles(CancellationToken cancellationToken)
    {
        // Get all files in the source directory
        var sourceFiles = Directory.GetFiles(_copyFromPath, "*", SearchOption.AllDirectories);

        foreach (var sourceFile in sourceFiles)
        {
            // Check if cancellation is requested before proceeding
            cancellationToken.ThrowIfCancellationRequested();

            // Relative path of the source file without the filename
            var sourceRelativePath = Path.GetRelativePath(_copyFromPath, Path.GetDirectoryName(sourceFile) ?? string.Empty);
            // Full replica file without the filename
            var replicaPath = Path.Combine(_copyToPath, sourceRelativePath);
            // Full replica file path including the filename
            var replicaFile = Path.Combine(replicaPath, Path.GetFileName(sourceFile));

            // Ensure the replica directory exists or create it
            if (!Directory.Exists(replicaPath))
            {
                try
                {
                    Directory.CreateDirectory(replicaPath);
                    await logWriter.LogAsync($"Created directory {replicaPath}");
                }
                catch (Exception ex)
                {
                    // Log any errors that occur during directory creation
                    await logWriter.LogAsync($"Error creating directory {replicaPath}: {ex.Message}");
                    continue; // Skip to the next file if there's an error
                }
            }

            if (!await CheckIfCopyFileNeeded(replicaPath, sourceFile, cancellationToken)) continue;

            using FileStream fs = File.Open(sourceFile, FileMode.Open, FileAccess.Read);
            using FileStream ds = File.Create(replicaFile);

            try
            {
                // Copy the file from source to replica
                await fs.CopyToAsync(ds, cancellationToken);
                await logWriter.LogAsync($"Copied file {sourceFile} to {_copyToPath}");
            }
            catch (Exception ex)
            {
                // Log any errors that occur during the copy operation
                await logWriter.LogAsync($"Error copying file {sourceFile}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Check the replica directory and remove files that no longer exist in the source directory.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task CleanupFiles(CancellationToken cancellationToken)
    {
        // Get all files in the replica directory
        var replicaFiles = Directory.GetFiles(_copyToPath, "*", SearchOption.AllDirectories);

        foreach (var replicaFile in replicaFiles)
        {
            // Check if cancellation is requested before proceeding
            cancellationToken.ThrowIfCancellationRequested();

            // Relative path of the replica file without the filename
            var replicaRelativePath = Path.GetRelativePath(_copyToPath, Path.GetDirectoryName(replicaFile) ?? string.Empty);
            var replicaPath = Path.Combine(_copyToPath, replicaRelativePath);
            var sourcePath = Path.Combine(_copyFromPath, replicaRelativePath);
            var sourceFile = Path.Combine(sourcePath, Path.GetFileName(replicaFile));

            // Check if the source directory exists and delete it if it does
            if (!Directory.Exists(sourcePath) && Directory.Exists(replicaPath))
            {
                try
                {
                    Directory.Delete(replicaPath, true);
                    await logWriter.LogAsync($"Deleted directory {replicaPath}");
                }
                catch (Exception ex)
                {
                    // Log any errors that occur during directory deletion
                    await logWriter.LogAsync($"Error deleting directory {replicaPath}: {ex.Message}");
                }

                continue; // Skip to the next file
            }

            // Check if the source file exists and delete it if it does
            if (!File.Exists(sourceFile) && File.Exists(replicaFile))
            {
                // File does not exist in the source, delete it from the replica
                try
                {
                    File.Delete(replicaFile);
                    await logWriter.LogAsync($"Deleted file {replicaFile}.");
                }
                catch (Exception ex)
                {
                    // Log any errors that occur during the deletion operation
                    await logWriter.LogAsync($"Error deleting file {replicaFile}: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// File comparision method modified implementation from the Microsoft documentation:
    /// https://learn.microsoft.com/en-us/troubleshoot/developer/visualstudio/csharp/language-compilers/create-file-compare
    /// </summary>
    /// <param name="filePath1"></param>
    /// <param name="filePath2"></param>
    /// <param name="cancellationToken"></param>
    /// <returns> true if the files are the same, false otherwise</returns>
    public async Task<bool> CompareFiles(string filePath1, string filePath2, CancellationToken cancellationToken)
    {
        int file1byte;
        int file2byte;
        await using FileStream fs1 = File.Open(filePath1, FileMode.Open, FileAccess.Read);
        await using FileStream fs2 = File.Open(filePath2, FileMode.Open, FileAccess.Read);

        // Check if the files are the same size
        if (fs1.Length != fs2.Length)
        {
            // If length is different, files are not the same
            return false;
        }

        // Read and compare the files byte by byte
        do
        {
            cancellationToken.ThrowIfCancellationRequested();

            file1byte = fs1.ReadByte();
            file2byte = fs2.ReadByte();
        }
        // Check if the bytes are equal and if we have not reached the end of either file
        // If cancellation is requested, break the loop and return true so it means that the files are equal (event if they are not) to avoid unnecessary check / copy process
        while (file1byte == file2byte && file1byte != -1);

        // If we reached the end of both files and all bytes were the same, they are equal
        return file1byte - file2byte == 0;
    }

    /// <summary>
    /// Checks if a file copy is needed by comparing the source file with the replica file.
    /// </summary>
    /// <param name="path">Replica path</param>
    /// <param name="sourceFile">Source filename</param>
    /// <returns></returns>
    public async Task<bool> CheckIfCopyFileNeeded(string path, string sourceFile, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var fullDestinationFilePath = Path.Combine(path, Path.GetFileName(sourceFile));

        if (!File.Exists(fullDestinationFilePath))
        {
            // File does not exist in the replica, copy is needed
            return true;
        }

        if (!await CompareFiles(sourceFile, fullDestinationFilePath, cancellationToken))
        {
            // Files are different, copy is needed
            return true;
        }

        // File exists and is the same, no copy needed
        return false;
    }
}