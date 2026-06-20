using Microsoft.Win32;
using System.Runtime.Versioning;

namespace TempCleanupService;

public static class UserTempFolderDiscovery
{
    private const string ProfileListRegistryPath =
        @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList";

    private static readonly HashSet<string> ExcludedProfileNames =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "All Users",
            "Default",
            "Default User",
            "Public",
            "systemprofile",
            "LocalService",
            "NetworkService"
        };

    public static IReadOnlyList<string> Discover()
    {
        if (!OperatingSystem.IsWindows())
        {
            return [Path.GetTempPath()];
        }

        var tempFolders = DiscoverFromRegistry();
        if (tempFolders.Count == 0)
        {
            tempFolders.AddRange(DiscoverFromDefaultUsersFolder());
        }

        return tempFolders
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    [SupportedOSPlatform("windows")]
    private static List<string> DiscoverFromRegistry()
    {
        var tempFolders = new List<string>();

        try
        {
            using var localMachine = RegistryKey.OpenBaseKey(
                RegistryHive.LocalMachine,
                RegistryView.Default);
            using var profileList = localMachine.OpenSubKey(ProfileListRegistryPath);

            if (profileList is null)
            {
                return tempFolders;
            }

            foreach (var profileKeyName in profileList.GetSubKeyNames())
            {
                using var profileKey = profileList.OpenSubKey(profileKeyName);
                var profileImagePath = profileKey?.GetValue("ProfileImagePath") as string;
                AddUserTempFolder(tempFolders, profileImagePath);
            }
        }
        catch (Exception ex) when (ex is IOException
            or UnauthorizedAccessException
            or System.Security.SecurityException)
        {
            return tempFolders;
        }

        return tempFolders;
    }

    private static IEnumerable<string> DiscoverFromDefaultUsersFolder()
    {
        var systemDrive = Environment.GetEnvironmentVariable("SystemDrive") ?? "C:";
        var usersFolder = Path.Combine(systemDrive, "Users");

        try
        {
            return Directory.EnumerateDirectories(usersFolder)
                .Select(GetUserTempFolder)
                .Where(tempFolder => tempFolder is not null)
                .Cast<string>()
                .ToArray();
        }
        catch (Exception ex) when (ex is IOException
            or UnauthorizedAccessException
            or System.Security.SecurityException)
        {
            return [];
        }
    }

    private static void AddUserTempFolder(List<string> tempFolders, string? profileImagePath)
    {
        var tempFolder = GetUserTempFolder(profileImagePath);
        if (tempFolder is not null)
        {
            tempFolders.Add(tempFolder);
        }
    }

    private static string? GetUserTempFolder(string? profileImagePath)
    {
        if (string.IsNullOrWhiteSpace(profileImagePath))
        {
            return null;
        }

        var expandedProfilePath = Environment.ExpandEnvironmentVariables(profileImagePath);
        var profileName = Path.GetFileName(
            expandedProfilePath.TrimEnd(Path.DirectorySeparatorChar));

        if (ExcludedProfileNames.Contains(profileName) || IsWindowsServiceProfile(expandedProfilePath))
        {
            return null;
        }

        var tempFolder = Path.Combine(expandedProfilePath, "AppData", "Local", "Temp");
        return Directory.Exists(tempFolder) ? tempFolder : null;
    }

    private static bool IsWindowsServiceProfile(string profilePath)
    {
        var windowsFolder = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        return !string.IsNullOrWhiteSpace(windowsFolder)
            && profilePath.StartsWith(
                windowsFolder + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase);
    }
}
