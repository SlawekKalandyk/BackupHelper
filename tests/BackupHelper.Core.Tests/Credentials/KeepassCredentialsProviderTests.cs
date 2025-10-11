using BackupHelper.Abstractions;
using BackupHelper.Core.Credentials;
using BackupHelper.Tests.Shared;

namespace BackupHelper.Core.Tests.Credentials;

[TestFixture]
public class KeePassCredentialsProviderTests : TestsBase
{
    [Test]
    public void GivenNoDatabase_WhenKeepassCredentialsProviderIsCreated_ThenDatabaseIsCreated()
    {
        var testDatabasePath = Path.Combine(TestsDirectoryRootPath, "test.kdbx");

        Assert.That(!File.Exists(testDatabasePath));

        using (_ = new KeePassCredentialsProvider(new(testDatabasePath, "testPassword"))) { }

        Assert.That(File.Exists(testDatabasePath));
    }

    [Test]
    public void GivenCredentials_WhenCredentialsAreSet_ThenSameCredentialsAreRetrieved()
    {
        var testDatabasePath = Path.Combine(TestsDirectoryRootPath, "test.kdbx");
        using var provider = new KeePassCredentialsProvider(new(testDatabasePath, "testPassword"));

        var expectedCredential = new CredentialEntry("TestCredential", "TestUser", "TestPass");
        provider.SetCredential(expectedCredential);

        var actualCredential = provider.GetCredential(expectedCredential.Title);

        Assert.That(actualCredential, Is.EqualTo(expectedCredential));
    }

    [Test]
    public void GivenDatabaseWithExistingCredentials_WhenSettingDuplicateCredentials_ThenExceptionIsThrown()
    {
        var testDatabasePath = Path.Combine(TestsDirectoryRootPath, "test.kdbx");
        using var provider = new KeePassCredentialsProvider(new(testDatabasePath, "testPassword"));

        var credential = new CredentialEntry("TestCredential", "TestUser", "TestPass");
        provider.SetCredential(credential);

        Assert.Throws<CredentialAlreadyExists>(() => provider.SetCredential(credential));
    }

    [Test]
    public void GivenDatabaseWithExistingCredentials_WhenDatabaseIsNewlyOpened_ThenSameCredentialsAreRetrieved()
    {
        var expectedCredential = new CredentialEntry("TestCredential", "TestUser", "TestPass");
        var testDatabasePath = Path.Combine(TestsDirectoryRootPath, "test.kdbx");

        using (var provider = new KeePassCredentialsProvider(new(testDatabasePath, "testPassword")))
        {
            provider.SetCredential(expectedCredential);
        }

        using (var provider = new KeePassCredentialsProvider(new(testDatabasePath, "testPassword")))
        {
            var actualCredential = provider.GetCredential(expectedCredential.Title);

            Assert.That(actualCredential, Is.EqualTo(expectedCredential));
        }
    }
}
