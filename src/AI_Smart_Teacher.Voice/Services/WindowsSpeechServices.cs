using AI_Smart_Teacher.Application.Voice;
using Windows.Globalization;
using Windows.Media.SpeechRecognition;
using Windows.Media.SpeechSynthesis;
using Windows.Storage.Streams;

namespace AI_Smart_Teacher.Voice.Services;

public sealed class WindowsSpeechToTextService : ISpeechToTextService
{
    public string Name => "Windows.Media.SpeechRecognition";
    public Task<bool> IsAvailableAsync(CancellationToken ct = default) => Task.FromResult(true);

    public async Task<SpeechToTextResult> RecognizeFromDefaultMicrophoneAsync(string? languageTag = null, CancellationToken ct = default)
    {
        SpeechRecognizer? recognizer = null;
        try
        {
            recognizer = string.IsNullOrWhiteSpace(languageTag)
                ? new SpeechRecognizer()
                : new SpeechRecognizer(new Language(languageTag));
            await recognizer.CompileConstraintsAsync().AsTask(ct);
            var result = await recognizer.RecognizeAsync().AsTask(ct);
            if (result.Status == SpeechRecognitionResultStatus.Success)
                return new SpeechToTextResult(true, null, result.Text?.Trim(), Name);
            return new SpeechToTextResult(false, "stt.status", null, Name, result.Status.ToString());
        }
        catch (UnauthorizedAccessException)
        {
            return new SpeechToTextResult(false, "stt.permission", null, Name, "Microphone/speech permission is not granted in Windows privacy settings.");
        }
        catch (OperationCanceledException)
        {
            return new SpeechToTextResult(false, "stt.cancelled", null, Name);
        }
        catch (Exception ex)
        {
            return new SpeechToTextResult(false, "stt.failed", null, Name, ex.GetType().Name);
        }
        finally
        {
            try { recognizer?.Dispose(); } catch { }
        }
    }
}

public sealed class WindowsTextToSpeechService : ITextToSpeechService
{
    private static readonly SpeechSynthesizer Synth = new();
    public string Name => "Windows.Media.SpeechSynthesis";
    public Task<bool> IsAvailableAsync(CancellationToken ct = default) => Task.FromResult(true);

    public async Task<TextToSpeechResult> SpeakAsync(string text, string? languageTag = null, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(text)) return new TextToSpeechResult(false, "tts.validation", Name);
            if (!string.IsNullOrWhiteSpace(languageTag))
            {
                var voice = SpeechSynthesizer.AllVoices.FirstOrDefault(v => string.Equals(v.Language, languageTag, StringComparison.OrdinalIgnoreCase));
                if (voice is not null) Synth.Voice = voice;
            }
            using SpeechSynthesisStream stream = await Synth.SynthesizeTextToStreamAsync(text).AsTask(ct);
            // UI layer plays audio; this service validates synthesis and produces a streamable result path in future if needed.
            _ = stream.Size;
            return new TextToSpeechResult(true, null, Name);
        }
        catch (OperationCanceledException)
        {
            return new TextToSpeechResult(false, "tts.cancelled", Name);
        }
        catch (Exception ex)
        {
            return new TextToSpeechResult(false, "tts.failed", Name, ex.GetType().Name);
        }
    }

    public async Task<Stream?> SynthesizeToStreamAsync(string text, string? languageTag = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        if (!string.IsNullOrWhiteSpace(languageTag))
        {
            var voice = SpeechSynthesizer.AllVoices.FirstOrDefault(v => string.Equals(v.Language, languageTag, StringComparison.OrdinalIgnoreCase));
            if (voice is not null) Synth.Voice = voice;
        }
        SpeechSynthesisStream stream = await Synth.SynthesizeTextToStreamAsync(text).AsTask(ct);
        return stream.AsStreamForRead();
    }
}
