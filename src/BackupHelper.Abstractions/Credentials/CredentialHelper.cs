namespace BackupHelper.Abstractions.Credentials;

public static class CredentialHelper
{
    public static (string Name, string LocalTitle) DeconstructCredentialTitle(string title)
    {
        var parts = title.Split('|', 2);

        if (parts.Length != 2)
        {
            throw new ArgumentException("Invalid credential title format.", nameof(title));
        }

        return (parts[0], parts[1]);
    }

    public static string ConstructCredentialTitle(string name, string localTitle)
    {
        return $"{name}|{localTitle}";
    }
}