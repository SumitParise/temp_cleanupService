namespace TempCleanupService;

public sealed class CleanupOptions
{
    public const string SectionName = "Cleanup";

    public double IntervalHours { get; set; } = 1;

    public string[] TargetFolders { get; set; } =
    [
        "%LOCALAPPDATA%\\Temp",
        "C:\\Windows\\Temp"
    ];

    public TimeSpan GetInterval()
    {
        return IntervalHours > 0
            ? TimeSpan.FromHours(IntervalHours)
            : TimeSpan.FromHours(1);
    }

    public IReadOnlyList<string> GetExpandedTargetFolders()
    {
        return TargetFolders
            .Where(folder => !string.IsNullOrWhiteSpace(folder))
            .Select(Environment.ExpandEnvironmentVariables)
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
