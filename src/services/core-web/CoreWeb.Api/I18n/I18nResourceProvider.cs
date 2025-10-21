using System.Collections.Concurrent;
using System.Text.Json;
using Core.Types.Dtos;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;

namespace CoreWeb.Api.I18n;

public interface II18nResourceProvider
{
    Task<I18nResourceDto> GetAsync(string language, string ns, CancellationToken cancellationToken);
}

public sealed class JsonFileI18nResourceProvider : II18nResourceProvider
{
    private readonly ConcurrentDictionary<string, I18nResourceDto> _cache = new();
    private readonly IWebHostEnvironment _environment;

    public JsonFileI18nResourceProvider(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public Task<I18nResourceDto> GetAsync(string language, string ns, CancellationToken cancellationToken)
    {
        var cacheKey = $"{language}:{ns}";
        if (_cache.TryGetValue(cacheKey, out var cached))
        {
            return Task.FromResult(cached);
        }

        var path = Path.Combine(_environment.ContentRootPath, "i18n", language, $"{ns}.json");
        IDictionary<string, object?> resources;
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            resources = JsonSerializer.Deserialize<Dictionary<string, object?>>(json) ?? new();
        }
        else
        {
            resources = new Dictionary<string, object?>();
        }

        var etag = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(resources)));
        var payload = new I18nResourceDto
        {
            Language = language,
            Namespace = ns,
            Resources = resources,
            ETag = etag
        };

        _cache[cacheKey] = payload;
        return Task.FromResult(payload);
    }
}
