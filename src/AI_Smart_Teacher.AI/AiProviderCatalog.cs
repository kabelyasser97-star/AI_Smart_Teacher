using AI_Smart_Teacher.Application.AI;

namespace AI_Smart_Teacher.AI;

public sealed class AiProviderCatalog
{
    private readonly IReadOnlyDictionary<string, IAiProvider> _providers;
    public AiProviderCatalog(IEnumerable<IAiProvider> providers)
    {
        _providers = providers.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyCollection<AiProviderInfo> List() => _providers.Values
        .Select(p => new AiProviderInfo(p.Name, p.DisplayName, p.RequiresApiKey, p is Providers.OpenAiCompatibleProvider))
        .ToArray();

    public IAiProvider Resolve(string? name)
    {
        if (!string.IsNullOrWhiteSpace(name) && _providers.TryGetValue(name, out var p)) return p;
        return _providers.TryGetValue("NotConfigured", out var fallback) ? fallback : _providers.Values.First();
    }
}
