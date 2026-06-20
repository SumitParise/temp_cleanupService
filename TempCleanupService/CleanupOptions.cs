namespace TempCleanupService;

public sealed class CleanupOptions
{
    public const string SectionName = "Cleanup";
    public const string AllUsersLocalTempToken = "{AllUsersLocalTemp}";

    public double IntervalHours { get; set; } = 1;

    public string[] TargetFolders { get; set; } =
    [
        AllUsersLocalTempToken,
        "C:\\Windows\\Temp"
    ];

    public TimeSpan GetInterval()
    {
        return IntervalHours > 0
            ? TimeSpan.FromHours(IntervalHours)
            : TimeSpan.FromHours(1);
    }

    public IReadOnlyList<string> GetExpandedTargetFolders(
        IEnumerable<string>? discoveredUserTempFolders = null)
    {
        var configuredFolders = TargetFolders
            .Where(folder => !string.IsNullOrWhiteSpace(folder))
            .ToArray();

        var userTempFolders = configuredFolders.Any(IsAllUsersLocalTempToken)
            ? discoveredUserTempFolders ?? UserTempFolderDiscovery.Discover()
            : [];

        return configuredFolders
            .SelectMany(folder => IsAllUsersLocalTempToken(folder) ? userTempFolders : [folder])
            .Select(Environment.ExpandEnvironmentVariables)
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool IsAllUsersLocalTempToken(string folder)
    {
        return string.Equals(
            folder,
            AllUsersLocalTempToken,
            StringComparison.OrdinalIgnoreCase);
    }
}
