using Microsoft.Extensions.Logging;

namespace BackupHelper.Core.FileZipping
{
    public class BackupFileZipper : IDisposable
    {
        private readonly BackupConfiguration _backupConfiguration;
        private readonly ILogger? _logger;
        private IFileZipper? _zipper;

        public BackupFileZipper(BackupConfiguration backupConfiguration, ILogger? logger = null)
        {
            _backupConfiguration = backupConfiguration;
            _logger = logger;
        }

        public void SaveZipFile(string savePath)
        {
            if (_zipper == null)
            {
                _zipper = new InMemoryFileZipper(_logger);
                var filePathMapping = MapFilePathsToZipPaths(_backupConfiguration.Directories, _backupConfiguration.Files);
                ZipBackupFiles(_zipper, filePathMapping);
            }

            if (_zipper.HasToBeSaved)
            {
                _zipper.Save(savePath, true);
            }
        }

        private void ZipBackupFiles(IFileZipper zipper, IDictionary<string, string> filePathMapping)
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

        public void Dispose()
        {
            _zipper?.Dispose();
        }
    }
}
