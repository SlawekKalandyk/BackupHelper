using System.IO.Compression;
using BackupHelper.Core.FileZipping;

namespace BackupHelper.Tests.FileZipping;

[TestFixture]
public class InMemoryFileZipperTests : FileZipperTestsBase
{
    protected override IFileZipper CreateFileZipper()
        => new InMemoryFileZipper();
}