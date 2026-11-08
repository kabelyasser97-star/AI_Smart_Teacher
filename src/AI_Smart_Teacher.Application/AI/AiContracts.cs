namespace AI_Smart_Teacher.Application.AI;

public sealed record AiProviderInfo(string Name, string DisplayName, bool RequiresApiKey, bool SupportsRemoteApi);
public sealed record AiChatRequest(Guid UserId, Guid? ConversationId, Guid? LessonId, string UserMessage, string? SystemContext = null);
public sealed record AiChatResult(bool Success, string? ErrorCode, Guid? ConversationId, string AssistantMessage, string ProviderName, string? ModelName = null);

public sealed record AiSettingsDto(string ActiveProvider, string? Model, string? Endpoint, bool HasApiKey, bool AllowCloudRequests, int MaxRequestsPerDay);

public interface IAiProvider
{
    string Name { get; }
    string DisplayName { get; }
    bool RequiresApiKey { get; }
    Task<AiChatResult> SendAsync(AiMessageContext context, CancellationToken ct = default);
}

public sealed record AiMessageContext(Guid UserId, Guid? LessonId, IReadOnlyList<(string Role, string Content)> History, string UserMessage, AiRuntimeSettings Settings);
public sealed record AiRuntimeSettings(string ProviderName, string? Model, string? Endpoint, string? ApiKey, bool AllowCloudRequests, int MaxRequestsPerDay);

public interface IAiSettingsService
{
    Task<AiSettingsDto> GetAsync(CancellationToken ct = default);
    Task SaveAsync(AiSettingsDto settings, string? newApiKey, CancellationToken ct = default);
    Task<AiRuntimeSettings> GetRuntimeAsync(CancellationToken ct = default);
}

public interface IConversationService
{
    Task<AiChatResult> SendMessageAsync(AiChatRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<ConversationSummaryDto>> GetRecentAsync(Guid userId, int take = 20, CancellationToken ct = default);
    Task<IReadOnlyList<ChatMessageDto>> GetMessagesAsync(Guid conversationId, Guid userId, CancellationToken ct = default);
}

public sealed record ConversationSummaryDto(Guid Id, string Title, DateTime UpdatedOrCreatedUtc, string ProviderName);
public sealed record ChatMessageDto(Guid Id, string Role, string Content, DateTime CreatedAtUtc, string? ModelName);
