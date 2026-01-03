using BackupHelper.Abstractions.Credentials;

namespace BackupHelper.Tests.Shared;

public class TestCredentialHandler : CredentialHandlerBase<TestCredential>
{
    public override string Kind => TestCredential.CredentialType;

    protected override TestCredential FromCredentialEntryCore(
        CredentialEntry entry,
        string localTitle
    ) => new(localTitle, entry.Username, entry.Password);

    protected override Task<bool> TestConnectionAsyncCore(
        TestCredential credential,
        CancellationToken cancellationToken
    ) => Task.FromResult(true);
}