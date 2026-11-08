using AI_Smart_Teacher.Application.Abstractions;
using AI_Smart_Teacher.Application.Vision;
using AI_Smart_Teacher.Core.Entities;
using AI_Smart_Teacher.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace AI_Smart_Teacher.Infrastructure.Vision;

public sealed class ImageAnalysisService : IImageAnalysisService
{
    private readonly AppDbContext _db;
    private readonly IAppPaths _paths;
    private readonly IOcrService _ocr;

    public ImageAnalysisService(AppDbContext db, IAppPaths paths, IOcrService ocr)
    {
        _db = db; _paths = paths; _ocr = ocr;
    }

    public async Task<ImageAnalysisResult> AnalyzeImageFileAsync(Guid userId, string sourceFilePath, Guid? conversationId = null, CancellationToken ct = default)
    {
        if (userId == Guid.Empty) return new ImageAnalysisResult(false, "vision.user_required", null, "User session is required.");
        if (string.IsNullOrWhiteSpace(sourceFilePath) || !File.Exists(sourceFilePath))
            return new ImageAnalysisResult(false, "vision.file_missing", null, "Image file was not found.");

        _paths.EnsureCreated();
        var ext = Path.GetExtension(sourceFilePath);
        var storedName = $"{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}{ext}";
        var storedPath = Path.Combine(_paths.AttachmentsDirectory, storedName);
        File.Copy(sourceFilePath, storedPath, overwrite: false);
        var info = new FileInfo(storedPath);

        Guid? messageId = null;
        if (conversationId.HasValue)
        {
            var conv = await _db.Conversations.FirstOrDefaultAsync(c => c.Id == conversationId.Value && c.UserId == userId, ct);
            if (conv is not null)
            {
                var msg = new ChatMessage { ConversationId = conv.Id, Role = "user", Content = $"[Image uploaded: {Path.GetFileName(sourceFilePath)}]" };
                _db.ChatMessages.Add(msg);
                messageId = msg.Id;
            }
        }

        var ocr = await _ocr.ExtractTextAsync(new OcrRequest(storedPath), ct);
        var attachment = new Attachment
        {
            ChatMessageId = messageId,
            FileName = Path.GetFileName(sourceFilePath),
            ContentType = GetContentType(ext),
            StoragePath = storedPath,
            SizeBytes = info.Exists ? info.Length : null,
            OcrText = ocr.Text,
            AnalysisResult = ocr.Success
                ? (string.IsNullOrWhiteSpace(ocr.Text) ? "Image stored. OCR ran but detected no text." : "Image stored. OCR text extracted.")
                : $"Image stored. OCR unavailable/failed: {ocr.ErrorCode ?? "unknown"} {ocr.Details}".Trim()
        };
        _db.Attachments.Add(attachment);
        await _db.SaveChangesAsync(ct);

        var summary = ocr.Success && !string.IsNullOrWhiteSpace(ocr.Text)
            ? $"OCR ({ocr.Engine}) extracted text. You can ask the AI Tutor to explain or solve it."
            : $"Image saved. OCR result: {ocr.ErrorCode ?? "ok"}. {ocr.Details}".Trim();
        return new ImageAnalysisResult(true, ocr.ErrorCode, attachment.Id, summary, ocr.Text);
    }

    private static string GetContentType(string ext) => ext.ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".bmp" => "image/bmp",
        ".gif" => "image/gif",
        ".webp" => "image/webp",
        _ => "application/octet-stream"
    };
}
