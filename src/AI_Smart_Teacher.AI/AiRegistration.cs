using AI_Smart_Teacher.AI.Providers;
using AI_Smart_Teacher.Application.AI;

namespace AI_Smart_Teacher.AI;

public static class AiRegistration
{
    public static IEnumerable<IAiProvider> CreateDefaultProviders() => new IAiProvider[]
    {
        new NotConfiguredProvider(),
        new OfflineStudyProvider(),
        new OpenAiCompatibleProvider()
    };
}
