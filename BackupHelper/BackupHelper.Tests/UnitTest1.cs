using BackupHelper.Core.FileZipping;

namespace BackupHelper.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            try
            {
                var zipConfig = new BackupConfiguration();
                var zipFile1 = new BackupFile(@"E:\Programming\tests\file1.txt");
                var zipDir1 = new BackupDirectory("dir11");
                var zipFile2 = new BackupFile(@"E:\Programming\tests\dir1");
                zipConfig.Files.Add(zipFile1);
                zipDir1.Files.Add(zipFile2);
                zipConfig.Directories.Add(zipDir1);
                var fileZipper = new BackupFileZipper(zipConfig);
                fileZipper.CreateZipFile(@"E:\Programming\tests\zipped-files.zip");
                //using var zipper = new Zipper(@"E:\Programming\tests\zipped-files.zip", true);
                //zipper.AddFile(@"E:\Programming\tests\file1.txt");
                //zipper.AddDirectory(@"E:\Programming\tests\dir1", @"dir11");

            }
            finally
            {
                //File.Delete(@"E:\Programming\tests\zipped-files.zip");
            }
        }

        [Fact]
        public void Test2() 
        {
            try
            {
                var zipConfig = new BackupConfiguration();
                var zipFile1 = new BackupFile(@"E:\Programming\tests\file1.txt");
                var zipDir1 = new BackupDirectory("dir11");
                var zipFile2 = new BackupFile(@"E:\Programming\tests\dir1");
                zipConfig.Files.Add(zipFile1);
                zipDir1.Files.Add(zipFile2);
                zipConfig.Directories.Add(zipDir1);
                zipConfig.ToJsonFile(@"E:\Programming\tests\config.json");

                var newZipConfig = BackupConfiguration.FromJsonFile(@"E:\Programming\tests\config.json");
                var fileZipper = new BackupFileZipper(newZipConfig);
                fileZipper.CreateZipFile(@"E:\Programming\tests\zipped-files.zip");
            }
            finally
            {
                File.Delete(@"E:\Programming\tests\zipped-files.zip");
            }
        }
    }
}