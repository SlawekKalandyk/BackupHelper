using SMBLibrary;
using SMBLibrary.Client;
using FileAttributes = SMBLibrary.FileAttributes;

namespace BackupHelper.Sources.SMB.IO;

public class SMBDirectory : SMBIOComponentBase
{
    private readonly string _directoryPath;

    private SMBDirectory(ISMBFileStore fileStore, object handle, FilePurpose filePurpose, string directoryPath)
        : base(fileStore, handle, filePurpose)
    {
        _directoryPath = directoryPath;
    }

    public IEnumerable<string> GetSubDirectories()
    {
        var fileInfos = GetFileInfos();

        return fileInfos
               .Where(info => (info.FileAttributes & FileAttributes.Directory) == FileAttributes.Directory)
               .Select(info => Path.Join(_directoryPath, info.FileName));
    }

    public IEnumerable<string> GetFiles()
    {
        var fileInfos = GetFileInfos();

        return fileInfos
               .Where(info => (info.FileAttributes & FileAttributes.Directory) != FileAttributes.Directory)
               .Select(info => Path.Join(_directoryPath, info.FileName));
    }

    public void Delete(bool recursive = false)
    {
        if ((FilePurpose & FilePurpose.Delete) != FilePurpose.Delete)
            throw new InvalidOperationException("This file cannot be deleted. It was not opened for deletion.");

        if (recursive)
            ClearDirectory();

        var fileDispositionInformation = new FileDispositionInformation
        {
            DeletePending = true,
        };
        var status = SMBFileStore.SetFileInformation(Handle, fileDispositionInformation);
        SMBHelper.ThrowIfStatusNotSuccess(status, nameof(SMBFileStore.SetFileInformation));
    }

    public void ClearDirectory()
    {
        var subDirectories = GetSubDirectories();

        foreach (var subDirectory in subDirectories)
        {
            using var subDir = OpenDirectoryForDeletion(SMBFileStore, subDirectory);
            subDir.Delete(recursive: true);
        }

        var files = GetFiles();

        foreach (var file in files)
        {
            using var smbFile = SMBFile.OpenFileForDeletion(SMBFileStore, file);
            smbFile.Delete();
        }
    }

    private IEnumerable<FileFullDirectoryInformation> GetFileInfos()
    {
        if ((FilePurpose & FilePurpose.Read) != FilePurpose.Read)
            throw new InvalidOperationException("This directory cannot be read. It was not opened for reading.");

        var status = SMBFileStore.QueryDirectory(
            out var fileInfoList,
            Handle,
            "*",
            FileInformationClass.FileFullDirectoryInformation);
        SMBHelper.ThrowIfStatusNotNoMoreFiles(status, nameof(SMBFileStore.QueryDirectory));

        var fileInfoCount = fileInfoList.Count;
        var castedFileInfoList = fileInfoList.Cast<FileFullDirectoryInformation>().ToList();
        var castedFileInfoCount = castedFileInfoList.Count;

        if (fileInfoCount != castedFileInfoCount)
        {
            throw new InvalidOperationException($"Mismatch in file information count: expected {fileInfoCount}, got {castedFileInfoCount}");
        }

        return RemoveDotDirectories(castedFileInfoList);
    }

    private List<FileFullDirectoryInformation> RemoveDotDirectories(List<FileFullDirectoryInformation> fileInfoList)
    {
        return fileInfoList
               .Where(info => info.FileName != "." && info.FileName != "..")
               .ToList();
    }

    public static SMBDirectory OpenDirectoryForReading(ISMBFileStore fileStore, string directoryPath)
    {
        var status = fileStore.CreateFile(
            out var fileHandle,
            out var fileStatus,
            directoryPath,
            AccessMask.GENERIC_READ | AccessMask.SYNCHRONIZE,
            FileAttributes.Directory,
            ShareAccess.Read,
            CreateDisposition.FILE_OPEN,
            CreateOptions.FILE_DIRECTORY_FILE | CreateOptions.FILE_SYNCHRONOUS_IO_NONALERT,
            null);
        SMBHelper.ThrowIfStatusNotSuccess(status, nameof(fileStore.CreateFile));
        SMBHelper.ThrowIfFileStatusNotFileOpened(fileStatus, directoryPath, nameof(fileStore.CreateFile));

        return new SMBDirectory(fileStore, fileHandle, FilePurpose.Read, directoryPath);
    }

    public static SMBDirectory OpenDirectoryForDeletion(ISMBFileStore fileStore, string directoryPath)
    {
        var status = fileStore.CreateFile(
            out var fileHandle,
            out var fileStatus,
            directoryPath,
            AccessMask.GENERIC_READ | AccessMask.DELETE | AccessMask.SYNCHRONIZE,
            FileAttributes.Directory,
            ShareAccess.None,
            CreateDisposition.FILE_OPEN,
            CreateOptions.FILE_DIRECTORY_FILE | CreateOptions.FILE_SYNCHRONOUS_IO_ALERT,
            null);
        SMBHelper.ThrowIfStatusNotSuccess(status, nameof(fileStore.CreateFile));
        SMBHelper.ThrowIfFileStatusNotFileOpened(fileStatus, directoryPath, nameof(fileStore.CreateFile));

        return new SMBDirectory(fileStore, fileHandle, FilePurpose.Read | FilePurpose.Delete, directoryPath);
    }

    public static SMBDirectory CreateDirectory(ISMBFileStore fileStore, string directoryPath)
    {
        var status = fileStore.CreateFile(
            out var fileHandle,
            out var fileStatus,
            directoryPath,
            AccessMask.GENERIC_READ | AccessMask.SYNCHRONIZE,
            FileAttributes.Directory,
            ShareAccess.Read,
            CreateDisposition.FILE_CREATE,
            CreateOptions.FILE_DIRECTORY_FILE | CreateOptions.FILE_SYNCHRONOUS_IO_NONALERT,
            null);
        SMBHelper.ThrowIfStatusNotSuccess(status, nameof(fileStore.CreateFile));
        SMBHelper.ThrowIfFileStatusNotFileCreated(fileStatus, directoryPath, nameof(fileStore.CreateFile));

        return new SMBDirectory(fileStore, fileHandle, FilePurpose.Read, directoryPath);
    }
}