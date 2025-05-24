using Microsoft.Extensions.Logging;
using System.IO.Compression;

namespace BackupHelper.Core.FileZipping
{
    public class InMemoryFileZipperFactory : IFileZipperFactory
    {
        private readonly ILogger<InMemoryFileZipper> _logger;

        public InMemoryFileZipperFactory(ILogger<InMemoryFileZipper> logger)
        {
            _logger = logger;
        }
        
        public IFileZipper Create(string zipFilePath, bool overwriteFileIfExists)
        {
            return new InMemoryFileZipper(_logger, zipFilePath, overwriteFileIfExists);
        }
    }

    public class InMemoryFileZipper : FileZipperBase
    {
        private readonly ILogger<InMemoryFileZipper> _logger;
        private readonly Stream _zipMemoryStream;
        private ZipArchive? _zipArchive;

        public InMemoryFileZipper(ILogger<InMemoryFileZipper> logger, string zipFilePath, bool overwriteFileIfExists)
            : base(zipFilePath, overwriteFileIfExists)
        {
            _logger = logger;
            _zipMemoryStream = new MemoryStream();
            _zipArchive = new ZipArchive(_zipMemoryStream, ZipArchiveMode.Create, leaveOpen: true);
        }

        public override void Save()
        {
            if (OverwriteFileIfExists && File.Exists(ZipFilePath))
            {
                _logger.LogWarning("Overwriting existing file: {ZipFilePath}", ZipFilePath);
                File.Delete(ZipFilePath);
            }

            // ZipArchive has to be disposed before underlying stream can be copied to a file
            _zipArchive?.Dispose();
            _zipArchive = null;
            using var fileStream = File.Open(ZipFilePath, FileMode.Create, FileAccess.ReadWrite);
            _zipMemoryStream.Seek(0, SeekOrigin.Begin);
            _zipMemoryStream.CopyTo(fileStream);
        }

        public override bool HasToBeSaved => true;

        public override void AddFile(string filePath, string zipPath = "")
        {
            EnsureZipArchiveIsOpen();

            var fileInfo = new FileInfo(filePath);
            var newZipPath = Path.Combine(zipPath, fileInfo.Name);
            try
            {
                _zipArchive!.CreateEntryFromFile(filePath, newZipPath, CompressionLevel.Optimal);
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to add file {FilePath} to zip file: {ExMessage}", filePath, e.Message);
            }
        }

        public override void AddDirectory(string directoryPath, string zipPath = "")
        {
            var directoryInfo = new DirectoryInfo(directoryPath);
            var newZipPath = Path.Combine(zipPath, directoryInfo.Name);

            _zipArchive!.CreateEntry(newZipPath + '/');
            AddDirectoryContent(directoryPath, newZipPath);
        }

        public override void AddDirectoryContent(string directoryPath, string zipPath = "")
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
            _zipArchive ??= new ZipArchive(_zipMemoryStream, ZipArchiveMode.Update, true);
        }

        public override void Dispose()
        {
            _zipArchive?.Dispose();
            _zipMemoryStream.Dispose();
        }
    }
}