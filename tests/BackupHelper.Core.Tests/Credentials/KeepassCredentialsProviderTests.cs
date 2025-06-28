using BackupHelper.Core.Credentials;
using BackupHelper.Tests.Shared;

namespace BackupHelper.Core.Tests.Credentials;

[TestFixture]
public class KeepassCredentialsProviderTests : TestsBase
{
    [Test]
    public void GivenNoDatabase_WhenKeepassCredentialsProviderIsCreated_ThenDatabaseIsCreated()
    {
        var testDatabasePath = Path.Combine(TestsDirectoryRootPath, "test.kdbx");

        Assert.That(!File.Exists(testDatabasePath));

        using (_ = new KeepassCredentialsProvider(testDatabasePath, "testPassword"))
        {
        }

        Assert.That(File.Exists(testDatabasePath));
    }

    [Test]
    public void GivenCredentials_WhenCredentialsAreSet_ThenSameCredentialsAreRetrieved()
    {
        var credentialName = "TestCredential";
        var expectedUsername = "TestUser";
        var expectedPassword = "TestPass";

        var testDatabasePath = Path.Combine(TestsDirectoryRootPath, "test.kdbx");
        using var provider = new KeepassCredentialsProvider(testDatabasePath, "testPassword");

        provider.SetCredential(credentialName, expectedUsername, expectedPassword);

        var (actualUsername, actualPassword) = provider.GetCredential(credentialName);

        Assert.That(actualUsername, Is.EqualTo(expectedUsername));
        Assert.That(actualPassword, Is.EqualTo(expectedPassword));
    }

    [Test]
    public void GivenDatabaseWithExistingCredentials_WhenSettingDuplicateCredentials_ThenExceptionIsThrown()
    {
        var credentialName = "TestCredential";
        var expectedUsername = "TestUser";
        var expectedPassword = "TestPass";

        var testDatabasePath = Path.Combine(TestsDirectoryRootPath, "test.kdbx");
        using var provider = new KeepassCredentialsProvider(testDatabasePath, "testPassword");

        provider.SetCredential(credentialName, expectedUsername, expectedPassword);

        Assert.Throws<CredentialAlreadyExists>(() => provider.SetCredential(credentialName, expectedUsername, expectedPassword));
    }

    [Test]
    public void GivenDatabaseWithExistingCredentials_WhenDatabaseIsNewlyOpened_ThenSameCredentialsAreRetrieved()
    {
        var credentialName = "TestCredential";
        var expectedUsername = "TestUser";
        var expectedPassword = "TestPass";

        var testDatabasePath = Path.Combine(TestsDirectoryRootPath, "test.kdbx");
        using (var provider = new KeepassCredentialsProvider(testDatabasePath, "testPassword"))
        {
            provider.SetCredential(credentialName, expectedUsername, expectedPassword);
        }

        using (var provider = new KeepassCredentialsProvider(testDatabasePath, "testPassword"))
        {
            var (actualUsername, actualPassword) = provider.GetCredential(credentialName);

            Assert.That(actualUsername, Is.EqualTo(expectedUsername));
            Assert.That(actualPassword, Is.EqualTo(expectedPassword));
        }
    }
}