using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TempCleanupService;

public sealed class TempCleanupRunner(
    IOptions<CleanupOptions> options,
    ILogger<TempCleanupRunner> logger)
{
    public async Task<CleanupResult> CleanAsync(CancellationToken cancellationToken)
    {
        var result = CleanupResult.Empty;

        foreach (var targetFolder in options.Value.GetExpandedTargetFolders())
        {
            cancellationToken.ThrowIfCancellationRequested();
            result = result.Add(await CleanFolderAsync(targetFolder, cancellationToken));
        }

        logger.LogInformation(
            "Cleanup completed. Deleted files: {DeletedFiles}, deleted folders: {DeletedDirectories}, skipped items: {SkippedItems}, missing folders: {MissingFolders}.",
            result.DeletedFiles,
            result.DeletedDirectories,
            result.SkippedItems,
            result.MissingFolders);

        return result;
    }

    private Task<CleanupResult> CleanFolderAsync(string targetFolder, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(targetFolder))
        {
            logger.LogWarning("Configured cleanup folder does not exist: {TargetFolder}", targetFolder);
            return Task.FromResult(new CleanupResult(0, 0, 0, 1));
        }

        logger.LogInformation("Cleaning temp folder: {TargetFolder}", targetFolder);

        var result = CleanupDirectoryContents(targetFolder, deleteCurrentDirectory: false, cancellationToken);
        return Task.FromResult(result);
    }

    private CleanupResult CleanupDirectoryContents(
        string directoryPath,
        bool deleteCurrentDirectory,
        CancellationToken cancellationToken)
    {
        var result = CleanupResult.Empty;

        foreach (var filePath in EnumerateFiles(directoryPath))
        {
            cancellationToken.ThrowIfCancellationRequested();
            result = TryDeleteFile(filePath) ? result with { DeletedFiles = result.DeletedFiles + 1 } : result with { SkippedItems = result.SkippedItems + 1 };
        }

        foreach (var childDirectory in EnumerateDirectories(directoryPath))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (IsReparsePoint(childDirectory))
            {
                result = TryDeleteDirectory(childDirectory) ? result with { DeletedDirectories = result.DeletedDirectories + 1 } : result with { SkippedItems = result.SkippedItems + 1 };
                continue;
            }

            var childResult = CleanupDirectoryContents(childDirectory, deleteCurrentDirectory: true, cancellationToken);
            result = result.Add(childResult);
        }

        if (deleteCurrentDirectory)
        {
            result = TryDeleteDirectory(directoryPath) ? result with { DeletedDirectories = result.DeletedDirectories + 1 } : result with { SkippedItems = result.SkippedItems + 1 };
        }

        return result;
    }

    private IEnumerable<string> EnumerateFiles(string directoryPath)
    {
        try
        {
            return Directory.EnumerateFiles(directoryPath).ToArray();
        }
        catch (Exception ex) when (IsSkippable(ex))
        {
            logger.LogWarning(ex, "Skipping files in folder: {DirectoryPath}", directoryPath);
            return [];
        }
    }

    private IEnumerable<string> EnumerateDirectories(string directoryPath)
    {
        try
        {
            return Directory.EnumerateDirectories(directoryPath).ToArray();
        }
        catch (Exception ex) when (IsSkippable(ex))
        {
            logger.LogWarning(ex, "Skipping child folders in folder: {DirectoryPath}", directoryPath);
            return [];
        }
    }

    private bool TryDeleteFile(string filePath)
    {
        try
        {
            File.SetAttributes(filePath, FileAttributes.Normal);
            File.Delete(filePath);
            logger.LogDebug("Deleted file: {FilePath}", filePath);
            return true;
        }
        catch (Exception ex) when (IsSkippable(ex))
        {
            logger.LogWarning(ex, "Skipping file: {FilePath}", filePath);
            return false;
        }
    }

    private bool TryDeleteDirectory(string directoryPath)
    {
        try
        {
            File.SetAttributes(directoryPath, FileAttributes.Normal);
            Directory.Delete(directoryPath, recursive: false);
            logger.LogDebug("Deleted folder: {DirectoryPath}", directoryPath);
            return true;
        }
        catch (Exception ex) when (IsSkippable(ex))
        {
            logger.LogWarning(ex, "Skipping folder: {DirectoryPath}", directoryPath);
            return false;
        }
    }

    private static bool IsReparsePoint(string directoryPath)
    {
        try
        {
            return new DirectoryInfo(directoryPath).Attributes.HasFlag(FileAttributes.ReparsePoint);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsSkippable(Exception ex)
    {
        return ex is IOException
            or UnauthorizedAccessException
            or System.Security.SecurityException
            or DirectoryNotFoundException
            or FileNotFoundException;
    }
}
