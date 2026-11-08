using System.IO;
using AI_Smart_Teacher.Application.AI;
using AI_Smart_Teacher.Application.Auth;
using AI_Smart_Teacher.Application.Vision;
using AI_Smart_Teacher.Application.Voice;
using AI_Smart_Teacher.Core.Security;
using Microsoft.Extensions.DependencyInjection;

namespace AI_Smart_Teacher.Desktop.ViewModels;

public sealed class AiChatViewModel
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICurrentUserSession _session;

    public AiChatViewModel(IServiceScopeFactory scopeFactory, ICurrentUserSession session)
    {
        _scopeFactory = scopeFactory;
        _session = session;
    }

    public Guid? ConversationId { get; private set; }
    public string Status { get; private set; } = string.Empty;
    public bool IsError { get; private set; }
    public bool CanUse => _session.HasPermission(AppPermissions.UseAiTutor);
    public bool CanManageSettings => _session.HasPermission(AppPermissions.ManageAiSettings);
    public List<ChatMessageDto> Messages { get; private set; } = new();
    public AiSettingsDto? Settings { get; private set; }
    public string? LastAssistantMessage => Messages.LastOrDefault(m => string.Equals(m.Role, "assistant", StringComparison.OrdinalIgnoreCase))?.Content;

    public async Task LoadSettingsAsync(CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        Settings = await scope.ServiceProvider.GetRequiredService<IAiSettingsService>().GetAsync(ct);
    }

    public async Task SaveSettingsAsync(AiSettingsDto settings, string? newApiKey, CancellationToken ct = default)
    {
        if (!CanManageSettings) { Status = "Permission denied."; IsError = true; return; }
        using var scope = _scopeFactory.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IAiSettingsService>().SaveAsync(settings, newApiKey, ct);
        Settings = await scope.ServiceProvider.GetRequiredService<IAiSettingsService>().GetAsync(ct);
        Status = "AI settings saved.";
        IsError = false;
    }

    public async Task RefreshConversationAsync(CancellationToken ct = default)
    {
        if (_session.User is null || ConversationId is null) { Messages = new(); return; }
        using var scope = _scopeFactory.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IConversationService>();
        Messages = (await svc.GetMessagesAsync(ConversationId.Value, _session.User.Id, ct)).ToList();
    }

    public async Task SendAsync(string text, CancellationToken ct = default)
    {
        if (_session.User is null) { Status = "Sign in required."; IsError = true; return; }
        if (!CanUse) { Status = "AI tutor permission is not enabled for this account."; IsError = true; return; }
        text = (text ?? string.Empty).Trim();
        if (text.Length == 0) return;
        using var scope = _scopeFactory.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IConversationService>();
        var result = await svc.SendMessageAsync(new AiChatRequest(_session.User.Id, ConversationId, null, text), ct);
        ConversationId = result.ConversationId ?? ConversationId;
        Status = result.Success ? string.Empty : (result.ErrorCode ?? "AI request failed.");
        IsError = !result.Success && result.ErrorCode != "ai.not_configured";
        await RefreshConversationAsync(ct);
    }

    public async Task AttachImageAndAskAsync(string imagePath, CancellationToken ct = default)
    {
        if (_session.User is null) { Status = "Sign in required."; IsError = true; return; }
        if (!CanUse) { Status = "AI tutor permission is not enabled for this account."; IsError = true; return; }
        using var scope = _scopeFactory.CreateScope();
        var vision = scope.ServiceProvider.GetRequiredService<IImageAnalysisService>();
        var analysis = await vision.AnalyzeImageFileAsync(_session.User.Id, imagePath, ConversationId, ct);
        if (!analysis.Success)
        {
            Status = analysis.ErrorCode ?? "Image analysis failed.";
            IsError = true;
            return;
        }
        ConversationId ??= await TryFindLatestConversationAsync(scope, ct);
        var prompt = string.IsNullOrWhiteSpace(analysis.OcrText)
            ? $"[Image: {Path.GetFileName(imagePath)}] {analysis.Summary} Please ask me to describe what kind of help I need with this image."
            : $"[Image OCR result from {Path.GetFileName(imagePath)}]\n{analysis.OcrText}\n\nPlease explain, correct, or solve this clearly for the student.";
        await SendWithScopeAsync(scope, prompt, ct);
        Status = string.IsNullOrWhiteSpace(analysis.OcrText) ? "Image saved; OCR found no text. You can ask a question." : "Image analyzed with OCR and sent to AI tutor.";
        IsError = false;
    }

    public async Task<Stream?> SynthesizeLastAssistantAsync(CancellationToken ct = default)
    {
        var text = LastAssistantMessage;
        if (string.IsNullOrWhiteSpace(text)) return null;
        using var scope = _scopeFactory.CreateScope();
        var tts = scope.ServiceProvider.GetRequiredService<ITextToSpeechService>();
        var lang = System.Globalization.CultureInfo.CurrentUICulture.Name.StartsWith("ar", StringComparison.OrdinalIgnoreCase) ? "ar-SA" : "en-US";
        return await tts.SynthesizeToStreamAsync(text.Length > 4000 ? text[..4000] : text, lang, ct);
    }

    private async Task SendWithScopeAsync(IServiceScope scope, string text, CancellationToken ct)
    {
        var svc = scope.ServiceProvider.GetRequiredService<IConversationService>();
        var result = await svc.SendMessageAsync(new AiChatRequest(_session.User!.Id, ConversationId, null, text), ct);
        ConversationId = result.ConversationId ?? ConversationId;
        Status = result.Success ? string.Empty : (result.ErrorCode ?? "AI request failed.");
        IsError = !result.Success && result.ErrorCode != "ai.not_configured";
        await RefreshConversationAsync(ct);
    }

    private async Task<Guid?> TryFindLatestConversationAsync(IServiceScope scope, CancellationToken ct)
    {
        if (_session.User is null) return null;
        var svc = scope.ServiceProvider.GetRequiredService<IConversationService>();
        var list = await svc.GetRecentAsync(_session.User.Id, 1, ct);
        return list.FirstOrDefault()?.Id;
    }

    public void NewConversation()
    {
        ConversationId = null;
        Messages = new();
        Status = string.Empty;
        IsError = false;
    }
}
