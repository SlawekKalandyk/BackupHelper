using System.IO.Compression;

namespace BackupHelper.Core.FileZipping
{
    public class FileZipper
    {
        private readonly IZipConfiguration _zipConfiguration;

        public FileZipper(IZipConfiguration zipConfiguration)
        {
            _zipConfiguration = zipConfiguration;
        }

        public void CreateZipFile(string savePath)
        {
            using var zipFile = File.Open(savePath, FileMode.CreateNew);
            using var zipArchive = new ZipArchive(zipFile, ZipArchiveMode.Create);

            CompressFilesFromNodes(zipArchive, _zipConfiguration.Nodes, string.Empty);
        }

        private void CompressFilesFromNodes(ZipArchive archive, IEnumerable<IZipConfigurationNode> nodes, string zipDirectoryPath)
        {
            foreach (var node in nodes)
            {
                if (node is BackupZipDirectory backupZipDirectory)
                {
                    var newZipDirectoryPath = Path.Combine(zipDirectoryPath, backupZipDirectory.Name);
                    CompressFilesFromNodes(archive, backupZipDirectory.Nodes, newZipDirectoryPath);
                }
                else if (node is BackupZipFile backupZipFile)
                {
                    if (Directory.Exists(backupZipFile.FilePath))
                    {
                        var newBackupZipDirectory = TransformFileSystemDirectoryToBackupZipDirectory(backupZipFile.FilePath);
                        var newZipDirectoryPath = Path.Combine(zipDirectoryPath, newBackupZipDirectory.Name);
                        CompressFilesFromNodes(archive, newBackupZipDirectory.Nodes, newZipDirectoryPath);
                    }
                    else
                    {
                        var trimmedFilePath = backupZipFile.FilePath
                            .Trim(Path.DirectorySeparatorChar)
                            .Trim(Path.AltDirectorySeparatorChar);
                        var fileName = Path.GetFileName(trimmedFilePath);
                        var zipFilePath = Path.Combine(zipDirectoryPath, fileName);
                        archive.CreateEntryFromFile(backupZipFile.FilePath, zipFilePath);
                    }
                }
            }
        }

        private BackupZipDirectory TransformFileSystemDirectoryToBackupZipDirectory(string dirPath)
        {
            var dirName = new DirectoryInfo(dirPath).Name;
            var backupZipDirectory = new BackupZipDirectory(dirName);

            var childFilePaths = Directory.GetFiles(dirPath);
            foreach (var childFilePath in childFilePaths)
            {
                var childBackupZipFile = new BackupZipFile(childFilePath);
                backupZipDirectory.Nodes.Add(childBackupZipFile);
            }

            var childDirPaths = Directory.GetDirectories(dirPath);
            foreach (var childDirPath in childDirPaths)
            {
                var childBackupZipDirectory = TransformFileSystemDirectoryToBackupZipDirectory(childDirPath);
                backupZipDirectory.Nodes.Add(childBackupZipDirectory);
            }

            return backupZipDirectory;
        }
    }
}
