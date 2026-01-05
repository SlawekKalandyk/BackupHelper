using BackupHelper.Abstractions.Credentials;

namespace BackupHelper.Core.Credentials;

public class CredentialHandlerRegistry
{
    private readonly IReadOnlyDictionary<string, ICredentialHandler> _handlers;

    public CredentialHandlerRegistry(IEnumerable<ICredentialHandler> handlers)
    {
        // throw if there are duplicate handler names
        var credentialFactories = handlers.ToList();

        if (
            credentialFactories
                .GroupBy(f => f.Kind, StringComparer.OrdinalIgnoreCase)
                .Any(g => g.Count() > 1)
        )
        {
            throw new ArgumentException("Duplicate credential handler names are not allowed.");
        }

        _handlers = credentialFactories.ToDictionary(f => f.Kind, StringComparer.OrdinalIgnoreCase);
    }

    public T FromCredentialEntry<T>(CredentialEntry entry)
        where T : ICredential
    {
        var credential = FromCredentialEntry(entry);

        if (credential is T typed)
            return typed;

        throw new NotSupportedException(
            $"Credential kind `{entry.EntryTitle.Kind}` does not produce `{typeof(T).Name}`."
        );
    }

    public bool TryGetCredentialFromEntry<T>(CredentialEntry entry, out T? credential)
        where T : ICredential
    {
        credential = default;

        if (!_handlers.TryGetValue(entry.EntryTitle.Kind, out var handler))
            return false;

        var createdCredential = handler.FromCredentialEntry(entry);

        if (createdCredential is T typed)
        {
            credential = typed;
            return true;
        }

        return false;
    }

    public IReadOnlyCollection<CredentialEntry> GetAllCredentialEntriesOfType<T>(
        IReadOnlyCollection<CredentialEntry> entries
    )
        where T : ICredential
    {
        var entriesOfType = new List<CredentialEntry>();

        foreach (var entry in entries)
        {
            if (TryGetCredentialFromEntry<T>(entry, out var credential) && credential != null)
            {
                entriesOfType.Add(entry);
            }
        }

        return entriesOfType;
    }

    public string GetKindFor<T>()
        where T : ICredential
    {
        var handler = _handlers.Values.OfType<ICredentialHandler<T>>().FirstOrDefault();

        if (handler == null)
            throw new NotSupportedException(
                $"No handler registered for credential type `{typeof(T).Name}`."
            );

        return handler.Kind;
    }

    public async Task<bool> TestConnectionAsync(
        CredentialEntry entry,
        CancellationToken cancellationToken = default
    )
    {
        var handler = GetHandlerForEntry(entry);
        return await handler.TestConnectionAsync(entry, cancellationToken);
    }

    private ICredential FromCredentialEntry(CredentialEntry entry)
    {
        var handler = GetHandlerForEntry(entry);
        return handler.FromCredentialEntry(entry);
    }

    private ICredentialHandler GetHandlerForEntry(CredentialEntry entry)
    {
        var kind = entry.EntryTitle.Kind;

        return !_handlers.TryGetValue(kind, out var handler)
            ? throw new NotSupportedException(
                $"No handler registered for credential kind `{kind}`."
            )
            : handler;
    }
}