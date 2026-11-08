namespace AI_Smart_Teacher.Application.Voice;

public sealed record SpeechToTextResult(bool Success, string? ErrorCode, string? Text, string Engine, string? Details = null);
public sealed record TextToSpeechResult(bool Success, string? ErrorCode, string Engine, string? Details = null);

public interface ISpeechToTextService
{
    string Name { get; }
    Task<bool> IsAvailableAsync(CancellationToken ct = default);
    Task<SpeechToTextResult> RecognizeFromDefaultMicrophoneAsync(string? languageTag = null, CancellationToken ct = default);
}

public interface ITextToSpeechService
{
    string Name { get; }
    Task<bool> IsAvailableAsync(CancellationToken ct = default);
    Task<TextToSpeechResult> SpeakAsync(string text, string? languageTag = null, CancellationToken ct = default);
    Task<Stream?> SynthesizeToStreamAsync(string text, string? languageTag = null, CancellationToken ct = default);
}
