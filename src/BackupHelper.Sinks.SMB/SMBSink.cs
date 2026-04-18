using System.Text;
using BackupHelper.Abstractions.Credentials;
using BackupHelper.Connectors.SMB;
using BackupHelper.Sinks.Abstractions;
using BackupHelper.Sinks.Abstractions.Retention;

namespace BackupHelper.Sinks.SMB;

public class SMBSink : SinkBase<SMBSinkDestination>, IPrunableSink
{
    private readonly string _server;
    private readonly string _shareName;
    private readonly string _username;
    private readonly SensitiveString _password;

    public SMBSink(
        SMBSinkDestination destination,
        string server,
        string shareName,
        string username,
        SensitiveString password
    )
        : base(destination)
    {
        _server = server;
        _shareName = shareName;
        _username = username;
        _password = password.Clone();
    }

    public override string Description =>
        $"SMB Sink to \\\\{TypedDestination.Server}\\{TypedDestination.ShareName}\\{TypedDestination.DestinationDirectory}".TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar
        );

    public override async Task UploadAsync(
        string sourceFilePath,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var connection = CreateConnection();
        EnsureDirectoryExists(connection, TypedDestination.DestinationDirectory);

        var destinationFilePath = GetDestinationFilePath(sourceFilePath);

        await using var sourceStream = new FileStream(
            sourceFilePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan
        );

        using var destinationStream = connection.CreateFile(destinationFilePath);
        await sourceStream.CopyToAsync(destinationStream, cancellationToken);
    }

    public override Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            using var connection = CreateConnection();
            return Task.FromResult(connection.TestConnection());
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public async Task PruneBackupsAsync(
        string uploadedBackupFileName,
        int maxBackups,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (maxBackups <= 0 || string.IsNullOrWhiteSpace(uploadedBackupFileName))
        {
            return;
        }

        using var connection = CreateConnection();
        EnsureDirectoryExists(connection, TypedDestination.DestinationDirectory);

        var manifestFilePath = GetFilePathInDestinationDirectory(
            BackupsRetentionConstants.ManifestFileName
        );
        var manifest = await ReadManifestAsync(connection, manifestFilePath, cancellationToken);
        var existingBackupFiles = GetExistingBackupFileNames(connection);
        var pruningPlan = BackupsRetentionPlanner.CreatePlanForCaseInsensitiveStorage(
            manifest,
            existingBackupFiles,
            uploadedBackupFileName,
            maxBackups
        );

        foreach (var backupFileName in pruningPlan.BackupFileNamesToDelete)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var backupFilePath = GetFilePathInDestinationDirectory(backupFileName);
            if (connection.FileExists(backupFilePath))
            {
                connection.DeleteFile(backupFilePath);
            }
        }

        await WriteManifestAsync(
            connection,
            manifestFilePath,
            pruningPlan.UpdatedManifest,
            cancellationToken
        );
    }

    public override void Dispose()
    {
        _password.Dispose();
        GC.SuppressFinalize(this);
    }

    private SMBConnection CreateConnection()
    {
        return new SMBConnection(_server, string.Empty, _shareName, _username, _password);
    }

    private HashSet<string> GetExistingBackupFileNames(SMBConnection connection)
    {
        var normalizedDirectoryPath = NormalizeDirectoryPath(TypedDestination.DestinationDirectory);

        return connection
            .GetFiles(normalizedDirectoryPath)
            .Select(Path.GetFileName)
            .Where(fileName => !string.IsNullOrWhiteSpace(fileName))
            .Select(fileName => fileName!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private async Task<BackupsManifest> ReadManifestAsync(
        SMBConnection connection,
        string manifestFilePath,
        CancellationToken cancellationToken
    )
    {
        if (!connection.FileExists(manifestFilePath))
        {
            return new BackupsManifest();
        }

        await using var stream = connection.GetStream(manifestFilePath);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var manifestJson = await reader.ReadToEndAsync(cancellationToken);
        return BackupsManifestJsonSerializer.DeserializeOrDefault(manifestJson);
    }

    private static async Task WriteManifestAsync(
        SMBConnection connection,
        string manifestFilePath,
        BackupsManifest manifest,
        CancellationToken cancellationToken
    )
    {
        if (connection.FileExists(manifestFilePath))
        {
            connection.DeleteFile(manifestFilePath);
        }

        var manifestJson = BackupsManifestJsonSerializer.Serialize(manifest);
        await using var stream = connection.CreateFile(manifestFilePath);
        using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: false);

        await writer.WriteAsync(manifestJson.AsMemory(), cancellationToken);
        await writer.FlushAsync(cancellationToken);
    }

    private string GetFilePathInDestinationDirectory(string fileName)
    {
        var normalizedDirectoryPath = NormalizeDirectoryPath(TypedDestination.DestinationDirectory);

        return string.IsNullOrEmpty(normalizedDirectoryPath)
            ? fileName
            : Path.Join(normalizedDirectoryPath, fileName);
    }

    private string GetDestinationFilePath(string sourceFilePath)
    {
        var fileName = Path.GetFileName(sourceFilePath);
        var normalizedDestinationDirectory = NormalizeDirectoryPath(
            TypedDestination.DestinationDirectory
        );

        return string.IsNullOrEmpty(normalizedDestinationDirectory)
            ? fileName
            : Path.Join(normalizedDestinationDirectory, fileName);
    }

    private static void EnsureDirectoryExists(SMBConnection connection, string destinationDirectory)
    {
        var normalizedDestinationDirectory = NormalizeDirectoryPath(destinationDirectory);

        if (string.IsNullOrEmpty(normalizedDestinationDirectory))
        {
            return;
        }

        var segments = normalizedDestinationDirectory.Split(
            [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
            StringSplitOptions.RemoveEmptyEntries
        );

        var currentPath = string.Empty;

        foreach (var segment in segments)
        {
            currentPath = string.IsNullOrEmpty(currentPath)
                ? segment
                : Path.Join(currentPath, segment);

            if (!connection.DirectoryExists(currentPath))
            {
                connection.CreateDirectory(currentPath);
            }
        }
    }

    private static string NormalizeDirectoryPath(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            return string.Empty;
        }

        return directoryPath.Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}
