using AI_Smart_Teacher.AI;
using AI_Smart_Teacher.Application.AI;
using AI_Smart_Teacher.Core.Entities;
using AI_Smart_Teacher.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace AI_Smart_Teacher.Infrastructure.AI;

public sealed class ConversationService : IConversationService
{
    private readonly AppDbContext _db;
    private readonly IAiSettingsService _settings;
    private readonly AiProviderCatalog _catalog;

    public ConversationService(AppDbContext db, IAiSettingsService settings, AiProviderCatalog catalog)
    {
        _db = db; _settings = settings; _catalog = catalog;
    }

    public async Task<AiChatResult> SendMessageAsync(AiChatRequest request, CancellationToken ct = default)
    {
        var msg = (request.UserMessage ?? string.Empty).Trim();
        if (request.UserId == Guid.Empty || msg.Length == 0) return new AiChatResult(false, "ai.validation", request.ConversationId, Local("أدخل رسالة صالحة.", "Enter a valid message."), "None");
        if (msg.Length > 4000) msg = msg[..4000];

        var runtime = await _settings.GetRuntimeAsync(ct);
        if (!await WithinDailyLimitAsync(request.UserId, runtime.MaxRequestsPerDay, ct))
            return new AiChatResult(false, "ai.limit", request.ConversationId, Local("تم الوصول إلى الحد اليومي لطلبات AI المحفوظة لهذا المستخدم.", "The daily saved AI request limit for this user has been reached."), runtime.ProviderName);

        Conversation conversation;
        if (request.ConversationId.HasValue)
        {
            conversation = await _db.Conversations.Include(c => c.Messages).FirstOrDefaultAsync(c => c.Id == request.ConversationId.Value && c.UserId == request.UserId, ct)
                ?? CreateConversation(request, runtime.ProviderName, msg);
            if (_db.Entry(conversation).State == EntityState.Detached) _db.Conversations.Add(conversation);
        }
        else
        {
            conversation = CreateConversation(request, runtime.ProviderName, msg);
            _db.Conversations.Add(conversation);
        }

        _db.ChatMessages.Add(new ChatMessage { ConversationId = conversation.Id, Conversation = conversation, Role = "user", Content = msg });
        await _db.SaveChangesAsync(ct);

        var history = await _db.ChatMessages.AsNoTracking().Where(m => m.ConversationId == conversation.Id).OrderBy(m => m.CreatedAtUtc)
            .Select(m => new ValueTuple<string, string>(m.Role, m.Content)).ToListAsync(ct);
        var provider = _catalog.Resolve(runtime.ProviderName);
        conversation.ProviderName = provider.Name;

        var result = await provider.SendAsync(new AiMessageContext(request.UserId, request.LessonId ?? conversation.LessonId, history, msg, runtime), ct);
        _db.ChatMessages.Add(new ChatMessage { ConversationId = conversation.Id, Role = "assistant", Content = result.AssistantMessage, ModelName = result.ModelName ?? runtime.Model });
        conversation.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return result with { ConversationId = conversation.Id, ProviderName = provider.Name };
    }

    public async Task<IReadOnlyList<ConversationSummaryDto>> GetRecentAsync(Guid userId, int take = 20, CancellationToken ct = default)
        => await _db.Conversations.AsNoTracking().Where(c => c.UserId == userId).OrderByDescending(c => c.UpdatedAtUtc ?? c.CreatedAtUtc)
            .Take(Math.Clamp(take, 1, 100))
            .Select(c => new ConversationSummaryDto(c.Id, c.Title, c.UpdatedAtUtc ?? c.CreatedAtUtc, c.ProviderName)).ToListAsync(ct);

    public async Task<IReadOnlyList<ChatMessageDto>> GetMessagesAsync(Guid conversationId, Guid userId, CancellationToken ct = default)
        => await _db.ChatMessages.AsNoTracking().Where(m => m.ConversationId == conversationId && m.Conversation!.UserId == userId)
            .OrderBy(m => m.CreatedAtUtc).Select(m => new ChatMessageDto(m.Id, m.Role, m.Content, m.CreatedAtUtc, m.ModelName)).ToListAsync(ct);

    private async Task<bool> WithinDailyLimitAsync(Guid userId, int max, CancellationToken ct)
    {
        var since = DateTime.UtcNow.Date;
        var count = await _db.ChatMessages.AsNoTracking().CountAsync(m => m.Role == "user" && m.CreatedAtUtc >= since && m.Conversation!.UserId == userId, ct);
        return count < max;
    }

    private static Conversation CreateConversation(AiChatRequest request, string provider, string firstMessage)
        => new() { UserId = request.UserId, LessonId = request.LessonId, ProviderName = provider, Title = firstMessage.Length <= 60 ? firstMessage : firstMessage[..60] + "…" };

    private static string Local(string ar, string en) => ar;
}
