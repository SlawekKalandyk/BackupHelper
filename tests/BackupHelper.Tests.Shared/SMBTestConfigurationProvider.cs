using System.Runtime.CompilerServices;
using System.Text;
using BackupHelper.Sources.SMB;
using Microsoft.Extensions.Configuration;

namespace BackupHelper.Tests.Shared;

public class SMBTestConfigurationProvider
{
    public SMBTestConfigurationProvider(IConfiguration configuration)
    {
        ServerAddress = configuration["SMB:server"];
        ThrowIfNullOrEmpty(ServerAddress);

        ShareName = configuration["SMB:share"];
        ThrowIfNullOrEmpty(ShareName);

        Username = configuration["SMB:username"];
        ThrowIfNullOrEmpty(Username);

        Password = configuration["SMB:password"];
        ThrowIfNullOrEmpty(Password);

        TestsDirectoryName = configuration["SMB:testsDirectory"];
        ThrowIfNullOrEmpty(TestsDirectoryName);
    }

    /// <summary>
    /// Server IP address
    /// </summary>
    public string ServerAddress { get; }

    /// <summary>
    /// Share name on the server
    /// </summary>
    public string ShareName { get; }

    /// <summary>
    /// Username for authentication
    /// </summary>
    public string Username { get; }

    /// <summary>
    /// Password for authentication
    /// </summary>
    public string Password { get; }

    /// <summary>
    /// Name of the directory where tests will be performed.
    /// This directory will be created on the share if it does not exist.
    /// </summary>
    public string TestsDirectoryName { get; }

    /// <summary>
    /// Full path to the SMB share.
    /// This path is constructed as \\ServerAddress\ShareName.
    /// </summary>
    /// <example>
    /// \\192.168.1.123\MyShare
    /// </example>
    public string SharePath => $@"\\{ServerAddress}\{ShareName}";

    /// <summary>
    /// Full path to the directory where tests will be performed.
    /// This path is constructed as \\ServerAddress\ShareName\TestsDirectoryName.
    /// </summary>
    /// <example>
    /// \\192.168.1.123\MyShare\Tests
    /// </example>
    public string TestsDirectoryPath => Path.Combine(SharePath, TestsDirectoryName);

    public SMBConnection GetSMBConnection()
    {
        return new SMBConnection(ServerAddress, string.Empty, ShareName, Username, Password);
    }

    public void CreateTestDirectory(string directoryPath)
    {
        using (var smbConnection = GetSMBConnection())
        {
            smbConnection.CreateDirectory(Path.Join(TestsDirectoryName, directoryPath));
        }
    }

    public void CreateTestFile(string filePath, string content = "")
    {
        using var smbConnection = GetSMBConnection();
        using var writeStream = smbConnection.CreateFile(Path.Join(TestsDirectoryName, filePath));

        if (!string.IsNullOrEmpty(content))
        {
            writeStream.Write(Encoding.UTF8.GetBytes(content));
        }
    }

    private void ThrowIfNullOrEmpty(
        string? value,
        [CallerArgumentExpression("value")] string? name = null
    )
    {
        if (string.IsNullOrEmpty(value))
            throw new ArgumentNullException(name, $"{name} cannot be null or empty");
    }
}
