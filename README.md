# Temp Cleanup Service

`.NET 10` Windows Service that continuously cleans configured temporary folders. It deletes everything it can and skips locked, in-use, missing, or access-denied items without showing dialogs.

## Configuration

Edit `TempCleanupService/appsettings.json` before publishing or edit the deployed `appsettings.json` next to the service executable.

```json
{
  "Cleanup": {
    "IntervalHours": 1,
    "TargetFolders": [
      "{AllUsersLocalTemp}",
      "C:\\Windows\\Temp"
    ]
  }
}
```

- `IntervalHours` controls how often cleanup runs. The default is `1`.
- `{AllUsersLocalTemp}` discovers real Windows user profiles at runtime and cleans each existing `AppData\\Local\\Temp` folder.
- `TargetFolders` also supports explicit paths and Windows environment variables.
- Windows service profiles such as `LocalSystem`, `LocalService`, and `NetworkService` are excluded from user-temp discovery.
- Locked or in-use files/folders are logged and retried on the next interval.

## Local Run

```powershell
dotnet run --project .\TempCleanupService\TempCleanupService.csproj
```

Stop with `Ctrl+C`.

## Publish

```powershell
dotnet publish .\TempCleanupService\TempCleanupService.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o .\publish
```

This creates a self-contained Windows package, so the destination computer does not need a separate .NET 10 installation.

## Install As Windows Service

Run PowerShell as Administrator:

```powershell
New-Service `
  -Name "TempCleanupService" `
  -BinaryPathName "C:\Path\To\publish\TempCleanupService.exe" `
  -DisplayName "Temp Cleanup Service" `
  -Description "Cleans configured temporary folders and skips locked items." `
  -StartupType Automatic

Start-Service TempCleanupService
```

## Manage Service

```powershell
Get-Service TempCleanupService
Stop-Service TempCleanupService
Start-Service TempCleanupService
```

## Uninstall

Run PowerShell as Administrator:

```powershell
Stop-Service TempCleanupService
sc.exe delete TempCleanupService
```

## Validate

```powershell
dotnet build
dotnet test
```
