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
        using (
            _ = new KeePassCredentialsProvider(
                new(testDatabasePath, "testPassword"),
                credentialsHandlerRegistry
            )
        ) { }

        Assert.That(File.Exists(testDatabasePath));
    }

    [Test]
    public void GivenCredentials_WhenCredentialsAreSet_ThenSameCredentialsAreRetrieved()
    {
        var testDatabasePath = Path.Combine(TestsDirectoryRootPath, "test.kdbx");
        var credentialsHandlerRegistry =
            ServiceScope.ServiceProvider.GetRequiredService<CredentialHandlerRegistry>();
        using var provider = new KeePassCredentialsProvider(
            new(testDatabasePath, "testPassword"),
            credentialsHandlerRegistry
        );

        var expectedCredential = TestCredential.CreateCredentialEntry(
            "TestCredential",
            "TestUser",
            "TestPass"
        );
        provider.SetCredential(expectedCredential);

        var actualCredential = provider
            .GetCredential<TestCredential>(expectedCredential.EntryTitle)
            ?.ToCredentialEntry();

        Assert.That(actualCredential, Is.EqualTo(expectedCredential));
    }

    [Test]
    public void GivenDatabaseWithExistingCredentials_WhenSettingDuplicateCredentials_ThenExceptionIsThrown()
    {
        var testDatabasePath = Path.Combine(TestsDirectoryRootPath, "test.kdbx");
        var credentialsHandlerRegistry =
            ServiceScope.ServiceProvider.GetRequiredService<CredentialHandlerRegistry>();
        using var provider = new KeePassCredentialsProvider(
            new(testDatabasePath, "testPassword"),
            credentialsHandlerRegistry
        );

        var credential = TestCredential.CreateCredentialEntry(
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
        var expectedCredential = TestCredential.CreateCredentialEntry(
            "TestCredential",
            "TestUser",
            "TestPass"
        );
        var testDatabasePath = Path.Combine(TestsDirectoryRootPath, "test.kdbx");
        var credentialsHandlerRegistry =
            ServiceScope.ServiceProvider.GetRequiredService<CredentialHandlerRegistry>();
        using (
            var provider = new KeePassCredentialsProvider(
                new(testDatabasePath, "testPassword"),
                credentialsHandlerRegistry
            )
        )
        {
            provider.SetCredential(expectedCredential);
        }

        using (
            var provider = new KeePassCredentialsProvider(
                new(testDatabasePath, "testPassword"),
                credentialsHandlerRegistry
            )
        )
        {
            var actualCredential = provider
                .GetCredential<TestCredential>(expectedCredential.EntryTitle)
                ?.ToCredentialEntry();

            Assert.That(actualCredential, Is.EqualTo(expectedCredential));
        }
    }
}