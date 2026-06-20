using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace TempCleanupService.Tests;

public sealed class TempCleanupRunnerTests
{
    [Fact]
    public async Task CleanAsync_DeletesFilesAndNestedFolders()
    {
        using var testFolder = new TempFolder();
        var nestedFolder = Directory.CreateDirectory(Path.Combine(testFolder.Path, "nested"));
        var filePath = Path.Combine(testFolder.Path, "test.tmp");
        var nestedFilePath = Path.Combine(nestedFolder.FullName, "nested.tmp");
        await File.WriteAllTextAsync(filePath, "temp");
        await File.WriteAllTextAsync(nestedFilePath, "nested");

        var result = await CreateRunner(testFolder.Path).CleanAsync(CancellationToken.None);

        Assert.Equal(2, result.DeletedFiles);
        Assert.Equal(1, result.DeletedDirectories);
        Assert.False(File.Exists(filePath));
        Assert.False(Directory.Exists(nestedFolder.FullName));
        Assert.True(Directory.Exists(testFolder.Path));
    }

    [Fact]
    public async Task CleanAsync_SkipsLockedFilesWithoutThrowing()
    {
        using var testFolder = new TempFolder();
        var lockedFilePath = Path.Combine(testFolder.Path, "locked.tmp");
        await File.WriteAllTextAsync(lockedFilePath, "locked");

        await using var lockedFile = new FileStream(
            lockedFilePath,
            FileMode.Open,
            FileAccess.ReadWrite,
            FileShare.None);

        var result = await CreateRunner(testFolder.Path).CleanAsync(CancellationToken.None);

        Assert.Equal(1, result.SkippedItems);
        Assert.True(File.Exists(lockedFilePath));
    }

    [Fact]
    public async Task CleanAsync_ContinuesAfterDeleteFailure()
    {
        using var testFolder = new TempFolder();
        var lockedFilePath = Path.Combine(testFolder.Path, "locked.tmp");
        var normalFilePath = Path.Combine(testFolder.Path, "normal.tmp");
        await File.WriteAllTextAsync(lockedFilePath, "locked");
        await File.WriteAllTextAsync(normalFilePath, "normal");

        await using var lockedFile = new FileStream(
            lockedFilePath,
            FileMode.Open,
            FileAccess.ReadWrite,
            FileShare.None);

        var result = await CreateRunner(testFolder.Path).CleanAsync(CancellationToken.None);

        Assert.Equal(1, result.DeletedFiles);
        Assert.Equal(1, result.SkippedItems);
        Assert.False(File.Exists(normalFilePath));
        Assert.True(File.Exists(lockedFilePath));
    }

    [Fact]
    public void CleanupOptions_ExpandsFoldersAndUsesConfiguredInterval()
    {
        var tempPath = Path.Combine("%TEMP%", "cleanup-service-test");
        var options = new CleanupOptions
        {
            IntervalHours = 2.5,
            TargetFolders = [tempPath]
        };

        var folders = options.GetExpandedTargetFolders();

        Assert.Equal(TimeSpan.FromHours(2.5), options.GetInterval());
        Assert.Single(folders);
        Assert.DoesNotContain("%TEMP%", folders[0]);
        Assert.EndsWith("cleanup-service-test", folders[0]);
    }

    [Fact]
    public void CleanupOptions_ResolvesAllUsersLocalTempAtRuntime()
    {
        var firstUserTemp = Path.GetFullPath(Path.Combine("profiles", "first", "Temp"));
        var secondUserTemp = Path.GetFullPath(Path.Combine("profiles", "second", "Temp"));
        var options = new CleanupOptions
        {
            TargetFolders =
            [
                CleanupOptions.AllUsersLocalTempToken,
                "%TEMP%"
            ]
        };

        var folders = options.GetExpandedTargetFolders([firstUserTemp, secondUserTemp]);

        Assert.Equal(3, folders.Count);
        Assert.Contains(firstUserTemp, folders);
        Assert.Contains(secondUserTemp, folders);
        Assert.Contains(
            Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar),
            folders);
    }

    private static TempCleanupRunner CreateRunner(string targetFolder)
    {
        var options = Options.Create(new CleanupOptions
        {
            IntervalHours = 1,
            TargetFolders = [targetFolder]
        });

        return new TempCleanupRunner(options, NullLogger<TempCleanupRunner>.Instance);
    }

    private sealed class TempFolder : IDisposable
    {
        public TempFolder()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "TempCleanupService.Tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
