using BackupHelper.Abstractions.Credentials;

namespace BackupHelper.Tests.Shared;

public class TestCredentialHandler : CredentialHandlerBase<TestCredential>
{
    public override string Kind => TestCredential.CredentialType;

    public override TestCredential FromCredentialEntry(CredentialEntry entry)
    {
        var title = entry.EntryTitle.Pairs[nameof(TestCredential.Title)];
        return new TestCredential(title, entry.Username, entry.Password);
    }

    protected override Task<bool> TestConnectionAsyncCore(
        TestCredential credential,
        CancellationToken cancellationToken
    ) => Task.FromResult(true);
}