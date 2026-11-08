using System.Text;
using AI_Smart_Teacher.Application.AI;

namespace AI_Smart_Teacher.AI.Providers;

public sealed class NotConfiguredProvider : IAiProvider
{
    public string Name => "NotConfigured";
    public string DisplayName => "Not configured / غير مضبوط";
    public bool RequiresApiKey => false;
    public Task<AiChatResult> SendAsync(AiMessageContext context, CancellationToken ct = default)
    {
        var ar = LooksArabic(context.UserMessage);
        var msg = ar
            ? "خدمة الذكاء الاصطناعي غير مضبوطة بعد. من تبويب الإعدادات (المرحلة القادمة للإدارة الكاملة) أو الإعدادات المحلية يمكنك تحديد مزود API ومفتاحه، مع العلم أن الخدمات السحابية تحتاج إنترنت ومفتاح API صالح ولا يتم تخزين المفاتيح داخل الكود. يمكنك الآن متابعة تصفح الدروس والمحتوى المحلي."
            : "The AI service is not configured yet. Configure an API provider and key in settings. Cloud AI requires internet and a valid API key; keys are not stored in source code. You can continue browsing local lessons and content.";
        return Task.FromResult(new AiChatResult(false, "ai.not_configured", null, msg, Name));
    }

    private static bool LooksArabic(string? text) => !string.IsNullOrWhiteSpace(text) && text.Any(c => c >= 0x0600 && c <= 0x06FF);
}

public sealed class OfflineStudyProvider : IAiProvider
{
    public string Name => "OfflineStudy";
    public string DisplayName => "Offline local study helper / مساعد محلي بدون إنترنت";
    public bool RequiresApiKey => false;
    public Task<AiChatResult> SendAsync(AiMessageContext context, CancellationToken ct = default)
    {
        var ar = LooksArabic(context.UserMessage);
        var sb = new StringBuilder();
        if (ar)
        {
            sb.AppendLine("وضع المساعد المحلي (بدون اتصال بمزود AI سحابي).");
            sb.AppendLine("سؤالك:");
            sb.AppendLine(context.UserMessage.Trim());
            sb.AppendLine();
            sb.AppendLine("خطوات مساعدة عامة:");
            sb.AppendLine("1) حدّد المطلوب من السؤال (شرح، حل، تعريف، مقارنة).");
            sb.AppendLine("2) راجع الدرس المرتبط والمصطلحات الأساسية.");
            sb.AppendLine("3) قسّم المسألة إلى خطوات صغيرة وتحقق من كل خطوة.");
            sb.AppendLine("4) إذا احتجت إجابة ذكية متقدمة أو تحليلًا أعمق، فعّل مزود AI سحابي في الإعدادات مع مفتاح API صالح واتصال إنترنت.");
        }
        else
        {
            sb.AppendLine("Local helper mode (no cloud AI provider call).");
            sb.AppendLine("Your question:");
            sb.AppendLine(context.UserMessage.Trim());
            sb.AppendLine();
            sb.AppendLine("General study steps:");
            sb.AppendLine("1) Identify what is requested (explain, solve, define, compare).");
            sb.AppendLine("2) Review the related lesson and key terms.");
            sb.AppendLine("3) Break the problem into small steps and verify each step.");
            sb.AppendLine("4) For advanced AI reasoning, configure a cloud AI provider with a valid API key and internet access.");
        }
        return Task.FromResult(new AiChatResult(true, null, null, sb.ToString().Trim(), Name, "local-rules"));
    }

    private static bool LooksArabic(string? text) => !string.IsNullOrWhiteSpace(text) && text.Any(c => c >= 0x0600 && c <= 0x06FF);
}
