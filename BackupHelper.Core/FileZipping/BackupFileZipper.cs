using Microsoft.Extensions.Logging;

namespace BackupHelper.Core.FileZipping
{
    public class BackupFileZipper
    {
        private readonly BackupConfiguration _backupConfiguration;
        private readonly ILogger? _logger;

        public BackupFileZipper(BackupConfiguration backupConfiguration, ILogger? logger = null)
        {
            _backupConfiguration = backupConfiguration;
            _logger = logger;
        }

        public void CreateZipFile(string savePath)
        {
            using var zipper = new Zipper(savePath, true, _logger);
            var filePathMapping = MapFilePathsToZipPaths(_backupConfiguration.Directories, _backupConfiguration.Files);
            ZipBackupFiles(zipper, filePathMapping);
        }

        private void ZipBackupFiles(Zipper zipper, IDictionary<string, string> filePathMapping)
        {
            foreach (var pair in filePathMapping)
            {
                if (Directory.Exists(pair.Key))
                    zipper.AddDirectory(pair.Key, pair.Value);
                else
                    zipper.AddFile(pair.Key, pair.Value);
            }
        }

        private IDictionary<string, string> MapFilePathsToZipPaths(ICollection<BackupDirectory> backupDirectories, ICollection<BackupFile> backupFiles)
        {
            var filePathMapping = new Dictionary<string, string>() as IDictionary<string, string>;
            TransformBackupDirectoriesToZipPaths(backupDirectories, filePathMapping);
            TransformBackupFilesToZipPaths(backupFiles, filePathMapping);

            return filePathMapping;
        }

        private void TransformBackupDirectoriesToZipPaths(ICollection<BackupDirectory> backupDirectories, IDictionary<string, string> paths, string zipPath = "")
        {
            foreach (var backupDirectory in backupDirectories)
            {
                var newZipPath = Path.Combine(zipPath, backupDirectory.Name);
                TransformBackupDirectoriesToZipPaths(backupDirectory.Directories, paths, newZipPath);
                TransformBackupFilesToZipPaths(backupDirectory.Files, paths, newZipPath);
            }
        }

        private static void TransformBackupFilesToZipPaths(ICollection<BackupFile> backupFiles, IDictionary<string, string> paths, string zipPath = "")
        {
            foreach (var backupFile in backupFiles)
            {
                paths[backupFile.FilePath] = zipPath;
            }
        }
    }
}
