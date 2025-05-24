using BackupHelper.Core.FileZipping;
using BackupHelper.Tests.Utilities;

namespace BackupHelper.Tests.FileZipping;

[TestFixture]
public class OnDiskFileZipperTests : FileZipperTestsBase
{
    protected override IFileZipper CreateFileZipperCore(string outputPath, bool overwriteFileIfExists)
        => new OnDiskFileZipper(new NullLogger<OnDiskFileZipper>(), outputPath, overwriteFileIfExists);
}