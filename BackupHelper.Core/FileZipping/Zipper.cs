using Microsoft.Extensions.Logging;
using System.IO.Compression;

namespace BackupHelper.Core.FileZipping
{
    public class Zipper : IDisposable
    {
        private readonly ILogger? _logger;
        private readonly Stream _zipFileStream;
        private ZipArchive? _zipArchive;

        public Zipper(ILogger? logger = null) 
        {
            _logger = logger;
            _zipFileStream = new MemoryStream();
            _zipArchive = new ZipArchive(_zipFileStream, ZipArchiveMode.Create, true);
        }

        public void Save(string zipFilePath, bool overwriteIfExists = false)
        {
            if (overwriteIfExists && File.Exists(zipFilePath))
                File.Delete(zipFilePath);

            // ZipArchive has to be disposed before underlying stream can be copied to a file
            _zipArchive?.Dispose();
            _zipArchive = null;

            using var fileStream = File.Open(zipFilePath, FileMode.Create, FileAccess.ReadWrite);
            _zipFileStream.Seek(0, SeekOrigin.Begin);
            _zipFileStream.CopyTo(fileStream);
        }

        public void AddFile(string filePath, string zipPath = "")
        {
            EnsureZipArchiveIsOpen();

            var fileInfo = new FileInfo(filePath);
            var newZipPath = Path.Combine(zipPath, fileInfo.Name);
            try
            {
                _zipArchive?.CreateEntryFromFile(filePath, newZipPath, CompressionLevel.Optimal);
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
            EnsureZipArchiveIsOpen();

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

        private void EnsureZipArchiveIsOpen()
        {
            _zipArchive ??= new ZipArchive(_zipFileStream, ZipArchiveMode.Update, true);
        }

        public void Dispose()
        {
            _zipArchive?.Dispose();
            _zipFileStream.Dispose();
        }
    }
}
