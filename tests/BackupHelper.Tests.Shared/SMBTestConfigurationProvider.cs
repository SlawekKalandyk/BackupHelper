using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using BackupHelper.Abstractions.Credentials;
using BackupHelper.Connectors.SMB;
using Microsoft.Extensions.Configuration;

namespace BackupHelper.Tests.Shared;

public class SMBTestConfigurationProvider
{
    public SMBTestConfigurationProvider(IConfiguration configuration)
    {
        var serverAddress = configuration["SMB:server"];
        ThrowIfNullOrEmpty(serverAddress);
        ServerAddress = serverAddress;

        var shareName = configuration["SMB:share"];
        ThrowIfNullOrEmpty(shareName);
        ShareName = shareName;

        var username = configuration["SMB:username"];
        ThrowIfNullOrEmpty(username);
        Username = username;

        var password = configuration["SMB:password"];
        ThrowIfNullOrEmpty(password);
        Password = password;

        var testsDirectoryName = configuration["SMB:testsDirectory"];
        ThrowIfNullOrEmpty(testsDirectoryName);
        TestsDirectoryName = testsDirectoryName;
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
        using var password = new SensitiveString(Password);
        return new SMBConnection(ServerAddress, string.Empty, ShareName, Username, password);
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
        [NotNull] string? value,
        [CallerArgumentExpression("value")] string? name = null
    )
    {
        if (string.IsNullOrEmpty(value))
            throw new ArgumentNullException(name, $"{name} cannot be null or empty");
    }
}