using System.IO.Compression;
using BackupHelper.Core.FileZipping;
using BackupHelper.Tests.Utilities;

namespace BackupHelper.Tests.FileZipping;

[TestFixture]
public class InMemoryFileZipperTests : FileZipperTestsBase
{
    protected override IFileZipper CreateFileZipper()
        => new InMemoryFileZipper(new NullLogger<InMemoryFileZipper>());
}