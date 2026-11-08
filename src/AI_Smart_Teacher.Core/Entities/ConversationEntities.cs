using AI_Smart_Teacher.Core.Enums;

namespace AI_Smart_Teacher.Core.Entities;

public class Conversation : EntityBase
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public Guid? LessonId { get; set; }
    public Lesson? Lesson { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ProviderName { get; set; } = "NotConfigured";
    public bool IsArchived { get; set; }
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}

public class ChatMessage : EntityBase
{
    public Guid ConversationId { get; set; }
    public Conversation? Conversation { get; set; }
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
    public string? ModelName { get; set; }
    public int? TokenCount { get; set; }
    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
}

public class Attachment : EntityBase
{
    public Guid? ChatMessageId { get; set; }
    public ChatMessage? ChatMessage { get; set; }
    public Guid? LessonContentId { get; set; }
    public LessonContent? LessonContent { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public long? SizeBytes { get; set; }
    public string? OcrText { get; set; }
    public string? AnalysisResult { get; set; }
}
