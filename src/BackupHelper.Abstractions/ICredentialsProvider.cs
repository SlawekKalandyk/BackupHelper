namespace BackupHelper.Abstractions;

public interface ICredentialsProvider : IDisposable
{
    (string Username, string Password) GetCredential(string credentialName);
    void SetCredential(string credentialName, string username, string password);
}