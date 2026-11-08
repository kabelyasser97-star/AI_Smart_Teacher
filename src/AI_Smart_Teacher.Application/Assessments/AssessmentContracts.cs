using AI_Smart_Teacher.Application.Curriculum;
using AI_Smart_Teacher.Core.Enums;

namespace AI_Smart_Teacher.Application.Assessments;

public sealed record AssessmentSummaryDto(Guid Id, string Title, string? Description, Guid? LessonId, Guid? SubjectId, bool IsPublished, int DurationMinutes, int QuestionCount);
public sealed record QuestionOptionDto(Guid Id, string Text, bool IsCorrect, int SortOrder);
public sealed record AssessmentQuestionDto(Guid Id, QuestionType QuestionType, string Prompt, string? Explanation, decimal Points, int SortOrder, IReadOnlyList<QuestionOptionDto> Options);
public sealed record AssessmentDetailDto(AssessmentSummaryDto Assessment, IReadOnlyList<AssessmentQuestionDto> Questions);
public sealed record AssessmentAnswerInput(Guid QuestionId, Guid? SelectedOptionId, string? AnswerText);
public sealed record SubmitAssessmentRequest(Guid UserId, Guid AssessmentId, IReadOnlyList<AssessmentAnswerInput> Answers);
public sealed record AssessmentResultDto(Guid Id, Guid AssessmentId, string AssessmentTitle, DateTime StartedAtUtc, DateTime? SubmittedAtUtc, decimal Score, decimal MaxScore, string? Feedback);
public sealed record StudentProgressDto(Guid? LessonId, Guid? SubjectId, int CompletionPercent, DateTime LastActivityAtUtc, string? Strengths, string? Weaknesses);

public interface IAssessmentService
{
    Task<IReadOnlyList<AssessmentSummaryDto>> ListAsync(CancellationToken ct = default);
    Task<AssessmentDetailDto?> GetAsync(Guid assessmentId, bool includeAnswersForManagement, CancellationToken ct = default);
    Task<OperationResult> CreateAssessmentAsync(string title, string? description, Guid? lessonId, Guid? subjectId, int durationMinutes, CancellationToken ct = default);
    Task<OperationResult> AddQuestionAsync(Guid assessmentId, QuestionType type, string prompt, decimal points, string? explanation, IReadOnlyList<(string Text, bool IsCorrect)> options, CancellationToken ct = default);
    Task<OperationResult> SetPublishedAsync(Guid assessmentId, bool published, CancellationToken ct = default);
    Task<(OperationResult Result, AssessmentResultDto? Score)> SubmitAsync(SubmitAssessmentRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<AssessmentResultDto>> GetMyResultsAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<StudentProgressDto>> GetMyProgressAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<AssessmentResultDto>> GetAllResultsAsync(CancellationToken ct = default);
}
