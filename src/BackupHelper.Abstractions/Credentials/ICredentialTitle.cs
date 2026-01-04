namespace BackupHelper.Abstractions.Credentials;

public interface ICredentialTitle
{
    string Kind { get; }

    CredentialEntryTitle ToCredentialEntryTitle();
}

public abstract record CredentialTitleBase : ICredentialTitle
{
    public override string ToString() => ToCredentialEntryTitle().ToString();

    public abstract string Kind { get; }

    public abstract IEnumerable<KeyValuePair<string, string>> GetTitleComponents();

    public CredentialEntryTitle ToCredentialEntryTitle() =>
        CredentialEntryTitle
            .Builder.AddRange(GetTitleComponents())
            .Add(NameValuePairHelper.ToNameValuePair(Kind))
            .Build();
}