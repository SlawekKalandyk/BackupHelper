using BackupHelper.Core.FileZipping;
using BackupHelper.Core.Tests.Utilities;

namespace BackupHelper.Core.Tests.FileZipping;

[TestFixture]
public class InMemoryFileZipperTests : FileZipperTestsBase
{
    protected override IFileZipper CreateFileZipperCore(string outputPath, bool overwriteFileIfExists)
        => new InMemoryFileZipper(new NullLogger<InMemoryFileZipper>(), outputPath, overwriteFileIfExists);
}