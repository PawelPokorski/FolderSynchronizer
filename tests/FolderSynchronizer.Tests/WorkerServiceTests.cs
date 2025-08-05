using FolderSynchronizer.Interfaces;
using FolderSynchronizer.Options;
using FolderSynchronizer.Services;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using NUnit.Framework;

namespace FolderSynchronizer.Tests;

/// <summary>
/// Simple unit tests for the WorkerService class
/// Mocks configured with the help of the article
/// https://www.codemag.com/Article/2305041/Using-Moq-A-Simple-Guide-to-Mocking-for-.NET
/// </summary>
public class WorkerServiceTests
{
    private Mock<ILogWriter> _logWriterMock = null!;
    private IOptions<WorkerOptions> _workerOptions = null!;
    private FileSynchronizationService _fileSynchronizationService = null!;

    private Mock<WorkerService> _workerServiceMock = null!;

    [SetUp]
    public void Setup()
    {
        var options = new WorkerOptions()
        {
            SyncInterval = 1000,
            LogFileFullPath = "test_logs/test_log.txt",
            CopyFromPath = "test_source",
            CopyToPath = "test_replica"
        };

        _workerOptions = Microsoft.Extensions.Options.Options.Create(options);
        _logWriterMock = new Mock<ILogWriter>();
        _logWriterMock.Setup(lw => lw.GetLogDirectory()).Returns(Path.GetDirectoryName(options.LogFileFullPath)!);
        _logWriterMock.Setup(lw => lw.GetLogFileName()).Returns(Path.GetFileName(options.LogFileFullPath)!);
        _fileSynchronizationService = new FileSynchronizationService(_workerOptions, _logWriterMock.Object);

        _workerServiceMock = new Mock<WorkerService>(_workerOptions, _fileSynchronizationService, _logWriterMock.Object);
    }

    [Test]
    public void WorkerServiceShouldLogErrorAndStopRunningWhenSourceDirectoryDoesNotExist()
    {
        if (Directory.Exists(_workerOptions.Value.CopyFromPath))
            Directory.Delete(_workerOptions.Value.CopyFromPath, true);

        _workerServiceMock.Object.CheckForDirectories();

        // Assert: Verify that the log writer was called with the expected message
        _logWriterMock.Verify(x => x.LogAsync(It.Is<string>(s => s.Contains("Source directory 'test_source' does not exist"))), Times.Once);

        // Assert: Verify that the application terminates with exit code -1
        _workerServiceMock.Protected().Verify("TerminateApplication", Times.Once(), ItExpr.Is<int>(exitCode => exitCode == -1));
    }

    [Test]
    public void WorkerServiceShouldCreateDirectoryAndLogWhenReplicaDirectoryDoesNotExist()
    {
        if (Directory.Exists(_workerOptions.Value.CopyToPath))
            Directory.Delete(_workerOptions.Value.CopyToPath, true);

        _workerServiceMock.Object.CheckForDirectories();

        // Assert: Verify that the log writer was called with the expected message
        _logWriterMock.Verify(x => x.LogAsync(It.Is<string>(s => s.Contains("Created replica directory 'test_replica'"))), Times.Once);

        // Assert: Verify that the replica directory was created
        Assert.That(Directory.Exists(_workerOptions.Value.CopyToPath), Is.True, "Replica directory should be created.");
    }
}
