using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AI_Smart_Teacher.Application.AI;

namespace AI_Smart_Teacher.AI.Providers;

public sealed class OpenAiCompatibleProvider : IAiProvider
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(60) };
    public string Name => "OpenAICompatible";
    public string DisplayName => "OpenAI-compatible API (cloud) / واجهة متوافقة مع OpenAI";
    public bool RequiresApiKey => true;

    public async Task<AiChatResult> SendAsync(AiMessageContext context, CancellationToken ct = default)
    {
        if (!context.Settings.AllowCloudRequests)
            return new AiChatResult(false, "ai.cloud_disabled", null, Local("الاتصال السحابي بالذكاء الاصطناعي غير مفعّل في الإعدادات.", "Cloud AI requests are disabled in settings."), Name);
        if (string.IsNullOrWhiteSpace(context.Settings.ApiKey))
            return new AiChatResult(false, "ai.api_key_missing", null, Local("مفتاح API غير مضبوط. لا يتم تخزين المفاتيح داخل الكود.", "API key is not configured. Keys are not stored in source code."), Name);
        if (string.IsNullOrWhiteSpace(context.Settings.Endpoint))
            return new AiChatResult(false, "ai.endpoint_missing", null, Local("عنوان خدمة API غير مضبوط.", "API endpoint is not configured."), Name);

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, context.Settings.Endpoint.Trim());
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.Settings.ApiKey);
            var messages = new List<object>
            {
                new { role = "system", content = "You are AI Smart Teacher. Be clear, educational, and safe. Answer in the user's language when possible." }
            };
            foreach (var (role, content) in context.History.TakeLast(12))
                messages.Add(new { role = role == "assistant" ? "assistant" : "user", content });
            messages.Add(new { role = "user", content = context.UserMessage });
            req.Content = JsonContent.Create(new { model = string.IsNullOrWhiteSpace(context.Settings.Model) ? "gpt-4o-mini" : context.Settings.Model, messages, temperature = 0.3 });

            using var resp = await Http.SendAsync(req, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
                return new AiChatResult(false, "ai.provider_error", null, Local($"فشل طلب مزود AI ({(int)resp.StatusCode}). تحقق من المفتاح/العنوان/الاتصال.", $"AI provider request failed ({(int)resp.StatusCode}). Check key/endpoint/network."), Name, context.Settings.Model);

            using var doc = JsonDocument.Parse(body);
            var text = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
            if (string.IsNullOrWhiteSpace(text))
                return new AiChatResult(false, "ai.empty", null, Local("لم يرجع المزود محتوى نصيًا.", "The provider returned no text content."), Name, context.Settings.Model);
            return new AiChatResult(true, null, null, text.Trim(), Name, context.Settings.Model);
        }
        catch (TaskCanceledException)
        {
            return new AiChatResult(false, "ai.timeout", null, Local("انتهت مهلة الاتصال بمزود AI أو انقطع الاتصال.", "The AI provider request timed out or the connection was interrupted."), Name, context.Settings.Model);
        }
        catch (HttpRequestException)
        {
            return new AiChatResult(false, "ai.network", null, Local("تعذر الاتصال بمزود AI. تحقق من الإنترنت وإعدادات الخدمة.", "Could not reach the AI provider. Check internet and service settings."), Name, context.Settings.Model);
        }
        catch (Exception)
        {
            return new AiChatResult(false, "ai.unexpected", null, Local("حدث خطأ أثناء معالجة طلب AI. لم يتم كشف تفاصيل حساسة.", "An error occurred while processing the AI request. Sensitive details are not exposed."), Name, context.Settings.Model);
        }
    }

    private static string Local(string ar, string en) => ar;
}
