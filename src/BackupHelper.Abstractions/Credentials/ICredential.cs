namespace BackupHelper.Abstractions.Credentials;

public interface ICredential
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

public abstract record CredentialBase<TTitle>(TTitle CredentialTitle) : ICredential<TTitle>
    where TTitle : ICredentialTitle
{
    ICredentialTitle ICredential.CredentialTitle => CredentialTitle;

    protected abstract string GetUsername();

    protected abstract string GetPassword();

    public CredentialEntry ToCredentialEntry()
    {
        return new CredentialEntry(
            CredentialTitle.ToCredentialEntryTitle(),
            GetUsername(),
            GetPassword()
        );
    }
}