# BackupHelper

BackupHelper is a .NET 9 backup utility for Windows that creates zip archives from local and SMB sources, then uploads the archive to one or more sinks:

- Local filesystem
- SMB share
- Azure Blob Storage

The app runs as an interactive wizard (console UI).

## What You Can Do

- Build a backup from files/directories (local and SMB)
- Organize archive layout using nested backup groups
- Use cron-based path resolution for date-stamped source paths
- Upload one backup to multiple destinations
- Apply per-sink retention with automatic pruning
- Store SMB and Azure credentials in KeePass-backed credential profiles

## Requirements

- Windows
- .NET SDK 9.0+
- Network access to any SMB/Azure targets you use
- Credentials for SMB shares and Azure Blob account(s), if applicable

## Build And Run

```bash
dotnet build
dotnet test
dotnet run --project src/BackupHelper.ConsoleApp
```

## Quick Start

1. Start the app.
2. In Main Menu, open Manage credential profiles and create a profile.
3. Add credentials you need:
   - SMB credentials are keyed by server + share
   - Azure credentials are keyed by account name
4. Create a backup plan JSON file (see Backup Plan Structure below).
5. In Main Menu, choose Create backup.
6. Point the wizard to your plan and working directory, then run.

Note: the generated zip is a staging file. After uploads finish (or fail), the local staging zip is deleted by the wizard flow.

## Backup Plan Structure

Backup plans are JSON objects with this top-level shape:

```json
{
  "items": [],
  "sinks": [],
  "logDirectory": "C:\\BackupLogs",
  "threadLimit": 2,
  "memoryLimitMB": 512,
  "compressionLevel": 6,
  "zipFileNameSuffix": "nightly",
  "sinkUploadParallelism": 2,
  "maxBackups": 14
}
```

### Top-Level Fields

| Field | Type | Required | Meaning |
|---|---|---|---|
| `items` | array | Yes | Files/directories to include in the zip. Supports nested groups. |
| `sinks` | array | Yes | Where to upload the resulting zip. |
| `logDirectory` | string or null | No | If set, backup logs are additionally written to rolling files in this directory. |
| `threadLimit` | int or null | No | Compression worker limit. `<= 1` behaves as single-threaded compression. |
| `memoryLimitMB` | int or null | No | Memory budget for parallel compression queue. `<= 0` means no memory limit. |
| `compressionLevel` | int or null | No | Default compression level for entries (expected range `0` to `9`). |
| `zipFileNameSuffix` | string or null | No | Suffix used in generated zip name: `yyyy-MM-dd_HH-mm-ss_<suffix>.zip`. Default suffix is `backup`. |
| `sinkUploadParallelism` | int or null | No | Max parallel uploads across sinks. Values `< 1` are coerced to `1`. |
| `maxBackups` | int or null | No | Global retention count for sinks that support pruning. Can be overridden per sink. |

### Items: Backup Entry Types

Each item in `items` is one of these two forms.

#### 1. File/Directory Entry

```json
{
  "path": "C:\\Data\\Reports",
  "cronExpression": "0 0 * * *",
  "compressionLevel": 9,
  "timeZone": "local"
}
```

Fields:

- `path` (required): file or directory path.
- `cronExpression` (optional): if set, BackupHelper computes the last scheduled occurrence and resolves date tokens in `path` before lookup.
- `compressionLevel` (optional): entry-specific compression level. Overrides top-level `compressionLevel`.
- `timeZone` (optional): `local` (default) or `utc` (case-insensitive).

Supported date tokens for cron-based path resolution:

- `%Y` year
- `%m` month (2-digit)
- `%d` day (2-digit)
- `%H` hour (2-digit)
- `%M` minute (2-digit)
- `%S` second (2-digit)

Example:

- Path template: `C:\Exports\auto-%Y-%m-%d_%H-%M`
- Cron: `0 0 * * *`
- Resolved path at runtime might become: `C:\Exports\auto-2026-04-18_00-00`

#### 2. Group/Directory-in-Archive Entry

```json
{
  "name": "Databases",
  "items": [
    {
      "path": "C:\\SqlBackups\\db1.bak"
    },
    {
      "path": "C:\\SqlBackups\\db2.bak"
    }
  ]
}
```

This creates a folder named `Databases` inside the zip and places child items under it.

### Sinks: Destination Types

Each sink must include `kind` and kind-specific fields.

#### FileSystem Sink

```json
{
  "kind": "FileSystem",
  "destinationDirectory": "D:\\Backups",
  "maxBackups": 30
}
```

Notes:

- Destination directory is created automatically during availability/upload if it does not exist.
- If upload succeeds, pruning uses `maxBackups` if enabled.

#### SMB Sink

```json
{
  "kind": "SMB",
  "server": "192.168.1.10",
  "shareName": "BackupShare",
  "destinationDirectory": "nightly/sql",
  "maxBackups": 14
}
```

Notes:

- Requires an SMB credential in the active credential profile with matching `server` + `shareName`.
- Destination subdirectories are created on upload if missing.

#### Azure Blob Storage Sink

```json
{
  "kind": "AzureBlobStorage",
  "accountName": "mybackupaccount",
  "container": "nightly",
  "maxBackups": 21
}
```

Notes:

- Requires an Azure credential in the active credential profile with matching `accountName`.
- The container must already exist, or upload is skipped as unavailable.

### Retention Behavior (`maxBackups`)

- Sink-level `maxBackups` overrides plan-level `maxBackups` for that sink.
- If effective `maxBackups` is null or `<= 0`, pruning is skipped.
- Pruning keeps most recent backups according to an internal manifest named `backups.json` in each sink target.

### Encryption Behavior

- During backup execution, the wizard asks whether to encrypt the resulting archive.
- If you choose encryption, the output is password-protected with AES-256 at zip level.

## Source Path Formats

BackupHelper supports source resolution by scheme.

- No scheme: defaults to local filesystem source
- `file://`: explicit local filesystem source
- `smb://`: explicit SMB source

### Local Examples

- `C:\Projects\App\appsettings.json`
- `C:\Projects\App\logs`
- `file://C:\Projects\App\logs`

### SMB Examples

For SMB source paths, use the `smb://` scheme with UNC path after it.

- `smb://\\192.168.1.10\Share\folder\file.txt`
- `smb://\\192.168.1.10\Share\folder`

Important: SMB source path parsing expects UNC share info in a format compatible with IP-based server parsing.

## Complete Backup Plan Example

```json
{
  "items": [
    {
      "name": "LocalData",
      "items": [
        {
          "path": "C:\\Data\\Invoices"
        },
        {
          "path": "C:\\Exports\\auto-%Y-%m-%d_%H-%M",
          "cronExpression": "0 0 * * *",
          "timeZone": "local",
          "compressionLevel": 5
        }
      ]
    },
    {
      "path": "smb://\\\\192.168.1.10\\Share\\sql\\full",
      "compressionLevel": 9
    }
  ],
  "sinks": [
    {
      "kind": "FileSystem",
      "destinationDirectory": "D:\\Backups\\Nightly",
      "maxBackups": 10
    },
    {
      "kind": "SMB",
      "server": "192.168.1.10",
      "shareName": "BackupShare",
      "destinationDirectory": "nightly",
      "maxBackups": 14
    },
    {
      "kind": "AzureBlobStorage",
      "accountName": "mybackupaccount",
      "container": "nightly",
      "maxBackups": 30
    }
  ],
  "logDirectory": "C:\\BackupHelper\\Logs",
  "threadLimit": 4,
  "memoryLimitMB": 1024,
  "compressionLevel": 6,
  "zipFileNameSuffix": "nightly",
  "sinkUploadParallelism": 3,
  "maxBackups": 7
}
```

## Credential Profiles And Backup Profiles

BackupHelper stores app data under:

- `%LOCALAPPDATA%\\BackupHelper\\CredentialProfiles`
- `%LOCALAPPDATA%\\BackupHelper\\BackupProfiles`

Operational guidance:

- Credential profiles store credentials in KeePass-backed files (password-protected).
- Backup profiles store references to:
  - backup plan location
  - credential profile name
  - working directory

## Troubleshooting

### "Unknown sink type"

Check `kind` in each sink entry. Supported values are exactly:

- `FileSystem`
- `SMB`
- `AzureBlobStorage`

### "Source with scheme '...' is not supported"

Supported source schemes are `file` and `smb`. For local paths, you can also omit scheme.

### Sink skipped as unavailable

- FileSystem: verify `destinationDirectory` path is valid and writable. Missing directories are created automatically.
- SMB: verify server/share reachability and matching SMB credential.
- Azure: verify container exists and account credential is present.

### Missing credential errors

Ensure active credential profile contains:

- SMB credential matching sink/source server + share
- Azure credential matching account name

### Invalid `timeZone`

Use only `local`, `utc`, or omit it.

### Cron parse errors

Use a valid Cronos-compatible cron expression (for example `0 0 * * *`).

## Security Notes

- Do not store secrets (passwords, SAS tokens) in backup plan JSON.
- Keep secrets only in credential profiles.
- Treat backup archives and logs as sensitive operational artifacts.
