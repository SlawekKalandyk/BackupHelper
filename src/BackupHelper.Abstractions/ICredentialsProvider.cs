namespace BackupHelper.Abstractions;

public interface ICredentialsProvider : IDisposable
{
    CredentialEntry? GetCredential(string credentialName);
    void SetCredential(CredentialEntry credentialEntry);
    void UpdateCredential(CredentialEntry credentialEntry);
    void DeleteCredential(string credentialName);
    IReadOnlyCollection<CredentialEntry> GetCredentials();
}
