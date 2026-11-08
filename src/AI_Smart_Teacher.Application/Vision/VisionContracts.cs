namespace AI_Smart_Teacher.Application.Vision;

public sealed record OcrRequest(string ImagePath, string? LanguageTag = null);
public sealed record OcrResult(bool Success, string? ErrorCode, string? Text, string Engine, string? Details = null);
public sealed record ImageAnalysisResult(bool Success, string? ErrorCode, Guid? AttachmentId, string Summary, string? OcrText = null);

public interface IOcrService
{
    string Name { get; }
    Task<OcrResult> ExtractTextAsync(OcrRequest request, CancellationToken ct = default);
}

public interface IImageAnalysisService
{
    Task<ImageAnalysisResult> AnalyzeImageFileAsync(Guid userId, string sourceFilePath, Guid? conversationId = null, CancellationToken ct = default);
}
