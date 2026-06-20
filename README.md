# Temp Cleanup Service

`.NET 10` Windows Service that continuously cleans configured temporary folders. It deletes everything it can and skips locked, in-use, missing, or access-denied items without showing dialogs.

## Configuration

Edit `TempCleanupService/appsettings.json` before publishing or edit the deployed `appsettings.json` next to the service executable.

```json
{
  "Cleanup": {
    "IntervalHours": 1,
    "TargetFolders": [
      "C:\\Users\\Sumit\\AppData\\Local\\Temp",
      "C:\\Windows\\Temp"
    ]
  }
}
```

- `IntervalHours` controls how often cleanup runs. The default is `1`.
- `TargetFolders` supports environment variables, but a service running as `LocalSystem` resolves `%LOCALAPPDATA%` to the system profile. Use an explicit user path such as `C:\\Users\\Sumit\\AppData\\Local\\Temp` to clean that user's temp folder.
- Locked or in-use files/folders are logged and retried on the next interval.

## Local Run

```powershell
dotnet run --project .\TempCleanupService\TempCleanupService.csproj
```

Stop with `Ctrl+C`.

## Publish

```powershell
dotnet publish .\TempCleanupService\TempCleanupService.csproj -c Release -r win-x64 --self-contained false -o .\publish
```

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
