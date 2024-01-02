using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using Microsoft.Extensions.Logging;

namespace BackupHelper.Core.FileZipping
{
    public class Zipper : IDisposable
    {
        private readonly FileStream _zipFileStream;
        private readonly ZipArchive _zipArchive;
        private readonly ILogger? _logger;

        public Zipper(string zipFilePath, bool overwriteIfExists = false, ILogger? logger = null) 
            : this(File.Open(zipFilePath, overwriteIfExists ? FileMode.Create : FileMode.OpenOrCreate, FileAccess.ReadWrite)
        )
        {
            _logger = logger;
        }

        public Zipper(FileStream zipFileStream)
        {
            _zipFileStream = zipFileStream;
            _zipArchive = new ZipArchive(_zipFileStream, ZipArchiveMode.Update);
        }

        public void AddFile(string filePath, string zipPath = "")
        {
            var fileInfo = new FileInfo(filePath);
            var newZipPath = Path.Combine(zipPath, fileInfo.Name);
            try
            {
                _zipArchive.CreateEntryFromFile(filePath, newZipPath, CompressionLevel.Optimal);
            }
            catch (Exception e)
            {
                _logger?.LogError($"Failed to add file {filePath} to zip file: {e.Message}");
            }
        }

        public void AddDirectory(string directoryPath, string zipPath = "")
        {
            var directoryInfo = new DirectoryInfo(directoryPath);
            var newZipPath = Path.Combine(zipPath, directoryInfo.Name);
            AddDirectoryCore(directoryPath, newZipPath);
        }

        public void AddDirectoryContent(string directoryPath, string zipPath = "")
        {
            AddDirectoryCore(directoryPath, zipPath);
        }

        private void AddDirectoryCore(string directoryPath, string zipPath)
        {
            var subDirectories = Directory.GetDirectories(directoryPath);
            var files = Directory.GetFiles(directoryPath);

            foreach (var subDirectoryPath in subDirectories)
            {
                AddDirectory(subDirectoryPath, zipPath);
            }

            foreach (var filePath in files)
            {
                AddFile(filePath, zipPath);
            }
        }

        public void Dispose()
        {
            _zipArchive?.Dispose();
            _zipFileStream?.Dispose();
        }
    }
}
