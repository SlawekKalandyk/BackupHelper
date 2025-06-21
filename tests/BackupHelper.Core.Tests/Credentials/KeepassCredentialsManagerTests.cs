using BackupHelper.Core.Credentials;

namespace BackupHelper.Core.Tests.Credentials;

[TestFixture]
public class KeepassCredentialsManagerTests : TestsBase
{
    [Test]
    public void GivenNoDatabase_WhenKeepassCredentialsManagerIsCreated_ThenDatabaseIsCreated()
    {
        var testDatabasePath = Path.Combine(TestsDirectoryRootPath, "test.kdbx");

        Assert.That(!File.Exists(testDatabasePath));

        using (_ = new KeepassCredentialsManager(testDatabasePath, "testPassword"))
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
        using var manager = new KeepassCredentialsManager(testDatabasePath, "testPassword");

        manager.SetCredential(credentialName, expectedUsername, expectedPassword);

        var (actualUsername, actualPassword) = manager.GetCredential(credentialName);

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
        using var manager = new KeepassCredentialsManager(testDatabasePath, "testPassword");

        manager.SetCredential(credentialName, expectedUsername, expectedPassword);

        Assert.Throws<CredentialAlreadyExists>(() => manager.SetCredential(credentialName, expectedUsername, expectedPassword));
    }

    [Test]
    public void GivenDatabaseWithExistingCredentials_WhenDatabaseIsNewlyOpened_ThenSameCredentialsAreRetrieved()
    {
        var credentialName = "TestCredential";
        var expectedUsername = "TestUser";
        var expectedPassword = "TestPass";

        var testDatabasePath = Path.Combine(TestsDirectoryRootPath, "test.kdbx");
        using (var manager = new KeepassCredentialsManager(testDatabasePath, "testPassword"))
        {
            manager.SetCredential(credentialName, expectedUsername, expectedPassword);
        }

        using (var manager = new KeepassCredentialsManager(testDatabasePath, "testPassword"))
        {
            var (actualUsername, actualPassword) = manager.GetCredential(credentialName);

            Assert.That(actualUsername, Is.EqualTo(expectedUsername));
            Assert.That(actualPassword, Is.EqualTo(expectedPassword));
        }
    }
}