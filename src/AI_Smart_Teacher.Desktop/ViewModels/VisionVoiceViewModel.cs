using AI_Smart_Teacher.Application.Auth;
using AI_Smart_Teacher.Application.Vision;
using AI_Smart_Teacher.Application.Voice;
using AI_Smart_Teacher.Core.Security;
using Microsoft.Extensions.DependencyInjection;

namespace AI_Smart_Teacher.Desktop.ViewModels;

public sealed class VisionVoiceViewModel
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICurrentUserSession _session;
    private readonly ITextToSpeechService _tts;
    private readonly ISpeechToTextService _stt;

    public VisionVoiceViewModel(IServiceScopeFactory scopeFactory, ICurrentUserSession session, ITextToSpeechService tts, ISpeechToTextService stt)
    {
        _scopeFactory = scopeFactory; _session = session; _tts = tts; _stt = stt;
    }

    public string Status { get; private set; } = string.Empty;
    public bool IsError { get; private set; }
    public string? LastOcrText { get; private set; }
    public bool CanUseAiFeatures => _session.HasPermission(AppPermissions.UseAiTutor);

    public async Task AnalyzeImageAsync(string filePath, Guid? conversationId = null, CancellationToken ct = default)
    {
        if (_session.User is null) { Status = "Sign in required."; IsError = true; return; }
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<IImageAnalysisService>();
            var result = await svc.AnalyzeImageFileAsync(_session.User.Id, filePath, conversationId, ct);
            LastOcrText = result.OcrText;
            Status = result.Summary + (string.IsNullOrWhiteSpace(result.OcrText) ? "" : $"\n\nOCR:\n{result.OcrText}");
            IsError = !result.Success;
        }
        catch (Exception ex)
        {
            IsError = true;
            Status = $"Image analysis failed: {ex.GetType().Name}";
        }
    }

    public async Task<string?> ListenAsync(string? languageTag = null, CancellationToken ct = default)
    {
        try
        {
            var result = await _stt.RecognizeFromDefaultMicrophoneAsync(languageTag, ct);
            if (result.Success) { Status = "Speech recognized."; IsError = false; return result.Text; }
            Status = $"Speech recognition unavailable/failed: {result.ErrorCode} {result.Details}".Trim();
            IsError = true;
            return null;
        }
        catch (Exception ex) { Status = $"Speech recognition failed: {ex.GetType().Name}"; IsError = true; return null; }
    }

    public async Task SpeakAsync(string text, string? languageTag = null, CancellationToken ct = default)
    {
        try
        {
            var result = await _tts.SpeakAsync(text, languageTag, ct);
            Status = result.Success ? "Speech synthesis completed." : $"Speech synthesis failed: {result.ErrorCode} {result.Details}".Trim();
            IsError = !result.Success;
        }
        catch (Exception ex) { Status = $"Speech synthesis failed: {ex.GetType().Name}"; IsError = true; }
    }
}
