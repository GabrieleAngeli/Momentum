namespace Identifier.Application.Caching;

public interface IIdentifierCache
{
    Task<TItem?> GetOrCreateAsync<TItem>(string key, Func<Task<TItem?>> factory, TimeSpan? ttl = null);
    void Remove(string key);
}
