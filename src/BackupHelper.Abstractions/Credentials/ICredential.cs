namespace BackupHelper.Abstractions.Credentials;

public interface ICredential : IAsyncDisposable, IDisposable
{
    ICredentialTitle CredentialTitle { get; }

    /// <summary>
    /// Converts the credential to a CredentialEntry for storage or transmission.
    /// </summary>
    CredentialEntry ToCredentialEntry();
}

public interface ICredential<TTitle> : ICredential
    where TTitle : ICredentialTitle
{
    new TTitle CredentialTitle { get; }
}

public abstract record CredentialBase<TTitle> : ICredential<TTitle>
    where TTitle : ICredentialTitle
{
    private readonly SensitiveString _password;

    protected CredentialBase(TTitle credentialTitle, SensitiveString password)
    {
        CredentialTitle = credentialTitle;
        _password = password;
    }

    public TTitle CredentialTitle { get; }
    ICredentialTitle ICredential.CredentialTitle => CredentialTitle;

    protected abstract string GetUsername();

    protected SensitiveString CredentialPassword => _password;

    public CredentialEntry ToCredentialEntry()
    {
        return new CredentialEntry(
            CredentialTitle.ToCredentialEntryTitle(),
            GetUsername(),
            _password.Clone()
        );
    }

    public virtual void Dispose()
    {
        _password.Dispose();
        GC.SuppressFinalize(this);
    }

    public virtual ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}