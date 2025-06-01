using System.IO.Compression;

namespace BackupHelper.Core.FileInUseZipEntryHandler;

public interface IFileInUseZipEntryHandlerFactory
{
    IFileInUseZipEntryHandler Create();
}

public interface IFileInUseZipEntryHandler : IDisposable
{
    void AddEntry(ZipArchive zipArchive, string filePath, string zipPath, CompressionLevel compressionLevel);
}