namespace BackupHelper.Core.Tests.Utilities;

public class TestDirectory : IDisposable
{
    private bool directoryHasBeenGenerated = false;

    public TestDirectory(string name, List<TestFile> files, List<TestDirectory> directories, string? zipPath = null)
    {
        Check.IsNull(name);
        Check.IsNull(files);
        Check.IsNull(directories);

        Name = name;
        Files = files;
        Directories = directories;
        ZipPath = zipPath;
    }

    public string Name { get; }
    public List<TestFile> Files { get; }
    public List<TestDirectory> Directories { get; }
    public string GeneratedDirectoryPath { get; private set; }
    public string UnzippedFilesDirectoryPath { get; private set; }
    public string? ZipPath { get; }

    public void Generate(string parentZipPath, string parentDirectoryPath)
    {
        Check.IsNullOrEmpty(parentDirectoryPath);

        GeneratedDirectoryPath = Path.Combine(parentDirectoryPath, Name);
        UnzippedFilesDirectoryPath = Path.Combine(parentZipPath, ZipPath ?? string.Empty, Name);

        Directory.CreateDirectory(GeneratedDirectoryPath);
        Files.ForEach(file => file.Generate(UnzippedFilesDirectoryPath, GeneratedDirectoryPath));
        Directories.ForEach(directory => directory.Generate(UnzippedFilesDirectoryPath, GeneratedDirectoryPath));

        directoryHasBeenGenerated = true;
    }

    public void AssertCorrectUnzippedFileStructure()
    {
        Assert.That(Directory.Exists(UnzippedFilesDirectoryPath));

        Files.ForEach(file => file.AssertCorrectUnzippedFileStructure());
        Directories.ForEach(directory => directory.AssertCorrectUnzippedFileStructure());
    }

    public void Dispose()
    {
        if (directoryHasBeenGenerated && Directory.Exists(GeneratedDirectoryPath))
        {
            Files.ForEach(file => file.Dispose());
            Directories.ForEach(directory => directory.Dispose());
            Directory.Delete(GeneratedDirectoryPath);
        }
    }
}

public class TestFile : IDisposable
{
    private const int blockSize = 1024 * 8;
    private const int blocksPerMb = (1024 * 1024) / blockSize;

    private bool fileHasBeenGenerated = false;

    public TestFile(string name, string? zipPath = null, int fileSizeInMb = 1)
    {
        Check.IsNull(name);
        Check.IsGreaterThanZero(fileSizeInMb);

        Name = name;
        ZipPath = zipPath;
        FileSizeInMb = fileSizeInMb;
    }

    public string Name { get; }
    public int FileSizeInMb { get; }
    public string GeneratedFilePath { get; private set; }
    public string UnzippedFilePath { get; private set; }
    public string? ZipPath { get; }

    public void Generate(string parentZipPath, string parentDirectoryPath)
    {
        Check.IsNullOrEmpty(parentDirectoryPath);

        GeneratedFilePath = Path.Combine(parentDirectoryPath, Name);
        UnzippedFilePath = Path.Combine(parentZipPath, ZipPath ?? string.Empty, Name);

        var data = new byte[blockSize];
        var rng = new Random();

        using (var stream = File.OpenWrite(GeneratedFilePath))
        {
            for (var i = 0; i < FileSizeInMb * blocksPerMb; i++)
            {
                rng.NextBytes(data);
                stream.Write(data, 0, data.Length);
            }
        }

        fileHasBeenGenerated = true;
    }

    public void AssertCorrectUnzippedFileStructure()
    {
        Assert.That(File.Exists(UnzippedFilePath));
    }

    public void Dispose()
    {
        if (fileHasBeenGenerated && File.Exists(GeneratedFilePath))
            File.Delete(GeneratedFilePath);
    }
}

public class TestFileStructure : IDisposable
{
    private bool fileStructureHasBeenGenerated = false;

    public TestFileStructure(List<TestFile> files, List<TestDirectory> directories)
    {
        Check.IsNull(files);
        Check.IsNull(directories);

        Files = files;
        Directories = directories;
    }

    public List<TestFile> Files { get; }
    public List<TestDirectory> Directories { get; }
    public string ZippedFilesDirectoryRootPath { get; private set; }
    public string UnzippedFilesDirectoryRootPath { get; private set; }

    public void Generate(string zippedFilesDirectoryRootPath, string unzippedFilesDirectoryRootPath)
    {
        Check.IsNull(zippedFilesDirectoryRootPath);
        Check.IsNull(unzippedFilesDirectoryRootPath);

        ZippedFilesDirectoryRootPath = zippedFilesDirectoryRootPath;
        UnzippedFilesDirectoryRootPath = unzippedFilesDirectoryRootPath;
        Files.ForEach(file => file.Generate(UnzippedFilesDirectoryRootPath, ZippedFilesDirectoryRootPath));
        Directories.ForEach(directory => directory.Generate(UnzippedFilesDirectoryRootPath, ZippedFilesDirectoryRootPath));

        fileStructureHasBeenGenerated = true;
    }

    public void AssertCorrectUnzippedFileStructure()
    {
        Files.ForEach(file => file.AssertCorrectUnzippedFileStructure());
        Directories.ForEach(directory => directory.AssertCorrectUnzippedFileStructure());
    }

    public void Dispose()
    {
        if (fileStructureHasBeenGenerated)
        {
            Files.ForEach(file => file.Dispose());
            Directories.ForEach(directory => directory.Dispose());
        }
    }
}