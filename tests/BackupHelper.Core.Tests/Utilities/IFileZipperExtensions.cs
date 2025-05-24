using BackupHelper.Core.FileZipping;

namespace BackupHelper.Core.Tests.Utilities;

public static class IFileZipperExtensions
{
    public static IFileZipper AddFile(this IFileZipper fileZipper, TestFile testFile)
    {
        fileZipper.AddFile(testFile.GeneratedFilePath, testFile.ZipPath ?? string.Empty);
        return fileZipper;
    }

    public static IFileZipper AddDirectory(this IFileZipper fileZipper, TestDirectory testDirectory)
    {
        fileZipper.AddDirectory(testDirectory.GeneratedDirectoryPath, testDirectory.ZipPath ?? string.Empty);
        return fileZipper;
    }

    public static IFileZipper AddDirectoryContent(this IFileZipper fileZipper, TestDirectory testDirectory)
    {
        fileZipper.AddDirectoryContent(testDirectory.GeneratedDirectoryPath, testDirectory.ZipPath ?? string.Empty);
        return fileZipper;
    }

    public static void AddDirectoryContent(this IFileZipper fileZipper, TestFileStructure testFileStructure)
    {
        fileZipper.AddDirectoryContent(testFileStructure.ZippedFilesDirectoryRootPath);
    }

    public static void AddTopLevelFilesAndDirectoriesSeparately(this IFileZipper fileZipper, TestFileStructure testFileStructure)
    {
        testFileStructure.Files.ForEach(testFile => fileZipper.AddFile(testFile));
        testFileStructure.Directories.ForEach(testDirectory => fileZipper.AddDirectory(testDirectory));
    }
}