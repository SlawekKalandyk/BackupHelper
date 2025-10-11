namespace BackupHelper.Core.Credentials;

public class CredentialNotFound(string credentialName)
    : Exception($"An entry with the name '{credentialName}' was not found.");
