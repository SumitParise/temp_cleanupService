namespace TempCleanupService;

public sealed record CleanupResult(
    int DeletedFiles,
    int DeletedDirectories,
    int SkippedItems,
    int MissingFolders)
{
    public static CleanupResult Empty { get; } = new(0, 0, 0, 0);

    public CleanupResult Add(CleanupResult other)
    {
        return new CleanupResult(
            DeletedFiles + other.DeletedFiles,
            DeletedDirectories + other.DeletedDirectories,
            SkippedItems + other.SkippedItems,
            MissingFolders + other.MissingFolders);
    }
}
