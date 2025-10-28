using Identifier.Application.Caching;
using Microsoft.Extensions.Caching.Memory;

namespace Identifier.Infrastructure.Caching;

public class MemoryIdentifierCache : IIdentifierCache
{
    private readonly IMemoryCache _memoryCache;

    public MemoryIdentifierCache(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public async Task<TItem?> GetOrCreateAsync<TItem>(string key, Func<Task<TItem?>> factory, TimeSpan? ttl = null)
    {
        if (_memoryCache.TryGetValue(key, out var cached) && cached is TItem typed)
        {
            return typed;
        }

        var created = await factory();
        if (created is not null)
        {
            _memoryCache.Set(key, created, ttl ?? TimeSpan.FromMinutes(5));
        }

        return created;
    }

    public void Remove(string key)
    {
        _memoryCache.Remove(key);
    }
}
