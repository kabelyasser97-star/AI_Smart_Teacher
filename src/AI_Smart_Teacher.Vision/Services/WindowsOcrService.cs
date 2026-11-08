using AI_Smart_Teacher.Application.Vision;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage;
using AppOcrResult = AI_Smart_Teacher.Application.Vision.OcrResult;
using WinOcrResult = Windows.Media.Ocr.OcrResult;

namespace AI_Smart_Teacher.Vision.Services;

public sealed class WindowsOcrService : IOcrService
{
    public string Name => "Windows.Media.Ocr";

    public async Task<AppOcrResult> ExtractTextAsync(OcrRequest request, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.ImagePath) || !File.Exists(request.ImagePath))
                return new AppOcrResult(false, "ocr.file_missing", null, Name, "Image file not found.");

            var file = await StorageFile.GetFileFromPathAsync(request.ImagePath).AsTask(ct);
            using var stream = await file.OpenReadAsync().AsTask(ct);
            var decoder = await BitmapDecoder.CreateAsync(stream).AsTask(ct);
            using var bitmap = await decoder.GetSoftwareBitmapAsync().AsTask(ct);

            var engine = TryCreateEngine(request.LanguageTag);
            if (engine is null)
                return new AppOcrResult(false, "ocr.language_unavailable", null, Name, "No Windows OCR language is available on this device. Install a supported Windows OCR language/resource, or use a future optional OCR provider.");

            WinOcrResult result = await engine.RecognizeAsync(bitmap).AsTask(ct);
            var text = string.Join(Environment.NewLine, result.Lines.Select(l => l.Text).Where(t => !string.IsNullOrWhiteSpace(t))).Trim();
            if (string.IsNullOrWhiteSpace(text))
                return new AppOcrResult(true, null, string.Empty, Name, "No text was detected in the image.");
            return new AppOcrResult(true, null, text, Name);
        }
        catch (OperationCanceledException)
        {
            return new AppOcrResult(false, "ocr.cancelled", null, Name);
        }
        catch (Exception ex)
        {
            return new AppOcrResult(false, "ocr.failed", null, Name, ex.GetType().Name);
        }
    }

    private static OcrEngine? TryCreateEngine(string? languageTag)
    {
        if (!string.IsNullOrWhiteSpace(languageTag))
        {
            try { return OcrEngine.TryCreateFromLanguage(new Language(languageTag)); } catch { }
        }
        var user = OcrEngine.TryCreateFromUserProfileLanguages();
        if (user is not null) return user;
        foreach (var tag in new[] { "ar", "en-US", "en" })
        {
            try
            {
                var engine = OcrEngine.TryCreateFromLanguage(new Language(tag));
                if (engine is not null) return engine;
            }
            catch { }
        }
        return null;
    }
}
