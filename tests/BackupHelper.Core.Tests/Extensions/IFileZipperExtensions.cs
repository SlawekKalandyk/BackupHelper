using BackupHelper.Core.FileZipping;
using BackupHelper.Tests.Shared;

namespace BackupHelper.Core.Tests.Extensions;

public static class IFileZipperExtensions
{
    public static async Task<IFileZipper> AddFileAsync(this IFileZipper fileZipper, TestFile testFile)
    {
        await fileZipper.AddFileAsync(testFile.GeneratedFilePath, testFile.ZipPath ?? string.Empty);
        return fileZipper;
    }

    public static async Task<IFileZipper> AddDirectoryAsync(
        this IFileZipper fileZipper,
        TestDirectory testDirectory
    )
    {
        await fileZipper.AddDirectoryAsync(
            testDirectory.GeneratedDirectoryPath,
            testDirectory.ZipPath ?? string.Empty
        );
        return fileZipper;
    }

    public static async Task<IFileZipper> AddDirectoryContentAsync(
        this IFileZipper fileZipper,
        TestDirectory testDirectory
    )
    {
        await fileZipper.AddDirectoryContentAsync(
            testDirectory.GeneratedDirectoryPath,
            testDirectory.ZipPath ?? string.Empty
        );
        return fileZipper;
    }

    public static Task AddDirectoryContentAsync(
        this IFileZipper fileZipper,
        TestFileStructure testFileStructure
    )
    {
        return fileZipper.AddDirectoryContentAsync(testFileStructure.ZippedFilesDirectoryRootPath);
    }

    public static async Task AddTopLevelFilesAndDirectoriesSeparatelyAsync(
        this IFileZipper fileZipper,
        TestFileStructure testFileStructure
    )
    {
        foreach (var testFile in testFileStructure.Files)
        {
            await fileZipper.AddFileAsync(testFile.GeneratedFilePath, testFile.ZipPath ?? string.Empty);
        }

        foreach (var testDirectory in testFileStructure.Directories)
        {
            await fileZipper.AddDirectoryAsync(
                testDirectory.GeneratedDirectoryPath,
                testDirectory.ZipPath ?? string.Empty
            );
        }
    }
}
