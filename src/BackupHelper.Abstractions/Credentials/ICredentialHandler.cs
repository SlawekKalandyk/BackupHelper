namespace BackupHelper.Abstractions.Credentials;

public interface ICredentialHandler
{
    string Kind { get; }
    ICredential FromCredentialEntry(CredentialEntry entry);

    Task<bool> TestConnectionAsync(
        CredentialEntry credentialEntry,
        CancellationToken cancellationToken = default
    );
}

public interface ICredentialHandler<T> : ICredentialHandler
    where T : ICredential
{
    new T FromCredentialEntry(CredentialEntry entry);
}

public abstract class CredentialHandlerBase<T> : ICredentialHandler<T>
    where T : ICredential
{
    public abstract string Kind { get; }

    ICredential ICredentialHandler.FromCredentialEntry(CredentialEntry entry) =>
        FromCredentialEntry(entry);

    public async Task<bool> TestConnectionAsync(
        CredentialEntry credentialEntry,
        CancellationToken cancellationToken = default
    ) => await TestConnectionAsyncCore(FromCredentialEntry(credentialEntry), cancellationToken);

    public T FromCredentialEntry(CredentialEntry entry)
    {
        var (_, localTitle) = CredentialHelper.DeconstructCredentialTitle(entry.Title);
        return FromCredentialEntryCore(entry, localTitle);
    }

    protected abstract T FromCredentialEntryCore(CredentialEntry entry, string localTitle);
    protected abstract Task<bool> TestConnectionAsyncCore(
        T credential,
        CancellationToken cancellationToken
    );
}
