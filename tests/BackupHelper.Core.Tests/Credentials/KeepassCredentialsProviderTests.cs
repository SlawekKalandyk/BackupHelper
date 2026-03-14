using BackupHelper.Abstractions.Credentials;
using BackupHelper.Core.Credentials;
using BackupHelper.Tests.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace BackupHelper.Core.Tests.Credentials;

[TestFixture]
public class KeePassCredentialsProviderTests : TestsBase
{
    [Test]
    public void GivenNoDatabase_WhenKeepassCredentialsProviderIsCreated_ThenDatabaseIsCreated()
    {
        var testDatabasePath = Path.Combine(TestsDirectoryRootPath, "test.kdbx");

        Assert.That(!File.Exists(testDatabasePath));

        var credentialsHandlerRegistry =
            ServiceScope.ServiceProvider.GetRequiredService<CredentialHandlerRegistry>();
        using var config = new KeePassCredentialsProviderConfiguration(
            testDatabasePath,
            new SensitiveString("testPassword")
        );
        using (_ = new KeePassCredentialsProvider(config, credentialsHandlerRegistry)) { }

        Assert.That(File.Exists(testDatabasePath));
    }

    [Test]
    public void GivenCredentials_WhenCredentialsAreSet_ThenSameCredentialsAreRetrieved()
    {
        var testDatabasePath = Path.Combine(TestsDirectoryRootPath, "test.kdbx");
        var credentialsHandlerRegistry =
            ServiceScope.ServiceProvider.GetRequiredService<CredentialHandlerRegistry>();
        using var config = new KeePassCredentialsProviderConfiguration(
            testDatabasePath,
            new SensitiveString("testPassword")
        );
        using var provider = new KeePassCredentialsProvider(config, credentialsHandlerRegistry);

        using var expectedCredential = TestCredential.CreateCredentialEntry(
            "TestCredential",
            "TestUser",
            "TestPass"
        );
        provider.SetCredential(expectedCredential);

        using var retrievedCredential = provider.GetCredential<TestCredential>(
            expectedCredential.EntryTitle
        );
        using var actualCredential = retrievedCredential?.ToCredentialEntry();

        Assert.That(actualCredential, Is.EqualTo(expectedCredential));
    }

    [Test]
    public void GivenDatabaseWithExistingCredentials_WhenSettingDuplicateCredentials_ThenExceptionIsThrown()
    {
        var testDatabasePath = Path.Combine(TestsDirectoryRootPath, "test.kdbx");
        var credentialsHandlerRegistry =
            ServiceScope.ServiceProvider.GetRequiredService<CredentialHandlerRegistry>();
        using var config = new KeePassCredentialsProviderConfiguration(
            testDatabasePath,
            new SensitiveString("testPassword")
        );
        using var provider = new KeePassCredentialsProvider(config, credentialsHandlerRegistry);

        using var credential = TestCredential.CreateCredentialEntry(
            "TestCredential",
            "TestUser",
            "TestPass"
        );
        provider.SetCredential(credential);

        Assert.Throws<CredentialAlreadyExists>(() => provider.SetCredential(credential));
    }

    [Test]
    public void GivenDatabaseWithExistingCredentials_WhenDatabaseIsNewlyOpened_ThenSameCredentialsAreRetrieved()
    {
        using var expectedCredential = TestCredential.CreateCredentialEntry(
            "TestCredential",
            "TestUser",
            "TestPass"
        );
        var testDatabasePath = Path.Combine(TestsDirectoryRootPath, "test.kdbx");
        var credentialsHandlerRegistry =
            ServiceScope.ServiceProvider.GetRequiredService<CredentialHandlerRegistry>();

        using var config = new KeePassCredentialsProviderConfiguration(
            testDatabasePath,
            new SensitiveString("testPassword")
        );

        using (
            var provider = new KeePassCredentialsProvider(config, credentialsHandlerRegistry)
        )
        {
            provider.SetCredential(expectedCredential);
        }

        using (
            var provider = new KeePassCredentialsProvider(config, credentialsHandlerRegistry)
        )
        {
            using var retrievedCredential = provider.GetCredential<TestCredential>(
                expectedCredential.EntryTitle
            );
            using var actualCredential = retrievedCredential?.ToCredentialEntry();

            Assert.That(actualCredential, Is.EqualTo(expectedCredential));
        }
    }
}