using AI_Smart_Teacher.Application.Assessments;
using AI_Smart_Teacher.Application.Curriculum;
using AI_Smart_Teacher.Core.Entities;
using AI_Smart_Teacher.Core.Enums;
using AI_Smart_Teacher.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace AI_Smart_Teacher.Infrastructure.Assessments;

public sealed class AssessmentService : IAssessmentService
{
    private readonly AppDbContext _db;
    public AssessmentService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<AssessmentSummaryDto>> ListAsync(CancellationToken ct = default)
        => await _db.Assessments.AsNoTracking().OrderByDescending(a => a.CreatedAtUtc)
            .Select(a => new AssessmentSummaryDto(a.Id, a.Title, a.Description, a.LessonId, a.SubjectId, a.IsPublished, a.DurationMinutes, a.Questions.Count(q => !q.IsDeleted)))
            .ToListAsync(ct);

    public async Task<AssessmentDetailDto?> GetAsync(Guid assessmentId, bool includeAnswersForManagement, CancellationToken ct = default)
    {
        var a = await _db.Assessments.AsNoTracking().Where(x => x.Id == assessmentId)
            .Select(x => new AssessmentSummaryDto(x.Id, x.Title, x.Description, x.LessonId, x.SubjectId, x.IsPublished, x.DurationMinutes, x.Questions.Count(q => !q.IsDeleted)))
            .FirstOrDefaultAsync(ct);
        if (a is null) return null;
        var qs = await _db.AssessmentQuestions.AsNoTracking().Where(q => q.AssessmentId == assessmentId).OrderBy(q => q.SortOrder)
            .Select(q => new AssessmentQuestionDto(q.Id, q.QuestionType, q.Prompt, includeAnswersForManagement ? q.Explanation : null, q.Points, q.SortOrder,
                q.Options.OrderBy(o => o.SortOrder).Select(o => new QuestionOptionDto(o.Id, o.Text, includeAnswersForManagement && o.IsCorrect, o.SortOrder)).ToList()))
            .ToListAsync(ct);
        return new AssessmentDetailDto(a, qs);
    }

    public async Task<OperationResult> CreateAssessmentAsync(string title, string? description, Guid? lessonId, Guid? subjectId, int durationMinutes, CancellationToken ct = default)
    {
        title = (title ?? string.Empty).Trim();
        if (title.Length < 2) return OperationResult.Fail("assessment.validation");
        if (lessonId.HasValue && !await _db.Lessons.AnyAsync(x => x.Id == lessonId.Value, ct)) return OperationResult.Fail("assessment.lesson_missing");
        if (subjectId.HasValue && !await _db.Subjects.AnyAsync(x => x.Id == subjectId.Value, ct)) return OperationResult.Fail("assessment.subject_missing");
        var a = new Assessment { Title = title, Description = Null(description), LessonId = lessonId, SubjectId = subjectId, DurationMinutes = Math.Clamp(durationMinutes, 1, 600), IsPublished = false };
        _db.Assessments.Add(a); await _db.SaveChangesAsync(ct); return OperationResult.Ok(a.Id);
    }

    public async Task<OperationResult> AddQuestionAsync(Guid assessmentId, QuestionType type, string prompt, decimal points, string? explanation, IReadOnlyList<(string Text, bool IsCorrect)> options, CancellationToken ct = default)
    {
        prompt = (prompt ?? string.Empty).Trim();
        if (prompt.Length < 2) return OperationResult.Fail("assessment.validation");
        var assessment = await _db.Assessments.Include(a => a.Questions).FirstOrDefaultAsync(a => a.Id == assessmentId, ct);
        if (assessment is null) return OperationResult.Fail("assessment.not_found");
        var q = new AssessmentQuestion { AssessmentId = assessmentId, QuestionType = type, Prompt = prompt, Explanation = Null(explanation), Points = points <= 0 ? 1 : Math.Round(points, 2), SortOrder = assessment.Questions.Count + 1 };
        if (type is QuestionType.MultipleChoice or QuestionType.TrueFalse)
        {
            var clean = options.Where(o => !string.IsNullOrWhiteSpace(o.Text)).Select((o, i) => new QuestionOption { Question = q, Text = o.Text.Trim(), IsCorrect = o.IsCorrect, SortOrder = i + 1 }).ToList();
            if (clean.Count < 2 || !clean.Any(o => o.IsCorrect)) return OperationResult.Fail("assessment.options_invalid");
            foreach (var o in clean) q.Options.Add(o);
        }
        _db.AssessmentQuestions.Add(q);
        await _db.SaveChangesAsync(ct);
        return OperationResult.Ok(q.Id);
    }

    public async Task<OperationResult> SetPublishedAsync(Guid assessmentId, bool published, CancellationToken ct = default)
    {
        var a = await _db.Assessments.FirstOrDefaultAsync(x => x.Id == assessmentId, ct);
        if (a is null) return OperationResult.Fail("assessment.not_found");
        a.IsPublished = published; a.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return OperationResult.Ok(a.Id);
    }

    public async Task<(OperationResult Result, AssessmentResultDto? Score)> SubmitAsync(SubmitAssessmentRequest request, CancellationToken ct = default)
    {
        if (request.UserId == Guid.Empty || request.AssessmentId == Guid.Empty) return (OperationResult.Fail("assessment.validation"), null);
        var assessment = await _db.Assessments.Include(a => a.Questions).ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(a => a.Id == request.AssessmentId && a.IsPublished, ct);
        if (assessment is null) return (OperationResult.Fail("assessment.not_found"), null);
        var byQ = request.Answers.GroupBy(a => a.QuestionId).ToDictionary(g => g.Key, g => g.Last());
        var result = new AssessmentResult { AssessmentId = assessment.Id, UserId = request.UserId, StartedAtUtc = DateTime.UtcNow, SubmittedAtUtc = DateTime.UtcNow };
        decimal score = 0, max = 0;
        foreach (var q in assessment.Questions.OrderBy(q => q.SortOrder))
        {
            max += q.Points;
            byQ.TryGetValue(q.Id, out var input);
            var ans = new StudentAnswer { AssessmentResult = result, QuestionId = q.Id, AnswerText = Null(input?.AnswerText), SelectedOptionId = input?.SelectedOptionId };
            if (q.QuestionType is QuestionType.MultipleChoice or QuestionType.TrueFalse)
            {
                var correct = q.Options.FirstOrDefault(o => o.IsCorrect)?.Id;
                ans.IsCorrect = correct.HasValue && input?.SelectedOptionId == correct;
                ans.PointsAwarded = ans.IsCorrect == true ? q.Points : 0;
            }
            else
            {
                ans.IsCorrect = null;
                ans.PointsAwarded = 0;
                ans.Feedback = "Manual review required / يحتاج مراجعة يدوية.";
            }
            score += ans.PointsAwarded ?? 0;
            result.Answers.Add(ans);
        }
        result.Score = Math.Round(score, 2);
        result.MaxScore = Math.Round(max, 2);
        result.Feedback = "Auto-scored objective questions. Subjective answers may require teacher review. النتيجة الآلية للأسئلة الموضوعية فقط.";
        _db.AssessmentResults.Add(result);
        await UpsertProgressAsync(request.UserId, assessment, score, max, ct);
        await _db.SaveChangesAsync(ct);
        return (OperationResult.Ok(result.Id), ToDto(result, assessment.Title));
    }

    public async Task<IReadOnlyList<AssessmentResultDto>> GetMyResultsAsync(Guid userId, CancellationToken ct = default)
        => await _db.AssessmentResults.AsNoTracking().Where(r => r.UserId == userId).OrderByDescending(r => r.SubmittedAtUtc ?? r.StartedAtUtc)
            .Select(r => new AssessmentResultDto(r.Id, r.AssessmentId, r.Assessment!.Title, r.StartedAtUtc, r.SubmittedAtUtc, r.Score ?? 0, r.MaxScore ?? 0, r.Feedback)).ToListAsync(ct);

    public async Task<IReadOnlyList<AssessmentResultDto>> GetAllResultsAsync(CancellationToken ct = default)
        => await _db.AssessmentResults.AsNoTracking().OrderByDescending(r => r.SubmittedAtUtc ?? r.StartedAtUtc)
            .Select(r => new AssessmentResultDto(r.Id, r.AssessmentId, r.Assessment!.Title, r.StartedAtUtc, r.SubmittedAtUtc, r.Score ?? 0, r.MaxScore ?? 0, r.Feedback)).ToListAsync(ct);

    public async Task<IReadOnlyList<StudentProgressDto>> GetMyProgressAsync(Guid userId, CancellationToken ct = default)
    {
        var profileId = await _db.StudentProfiles.AsNoTracking().Where(p => p.UserId == userId).Select(p => p.Id).FirstOrDefaultAsync(ct);
        if (profileId == Guid.Empty) return Array.Empty<StudentProgressDto>();
        return await _db.StudentProgress.AsNoTracking().Where(p => p.StudentProfileId == profileId).OrderByDescending(p => p.LastActivityAtUtc)
            .Select(p => new StudentProgressDto(p.LessonId, p.SubjectId, p.CompletionPercent, p.LastActivityAtUtc, p.Strengths, p.Weaknesses)).ToListAsync(ct);
    }

    private async Task UpsertProgressAsync(Guid userId, Assessment assessment, decimal score, decimal max, CancellationToken ct)
    {
        var profile = await _db.StudentProfiles.FirstOrDefaultAsync(p => p.UserId == userId, ct);
        if (profile is null)
        {
            profile = new StudentProfile { UserId = userId };
            _db.StudentProfiles.Add(profile);
            await _db.SaveChangesAsync(ct);
        }
        var pct = max <= 0 ? 0 : (int)Math.Clamp(Math.Round(score * 100m / max), 0, 100);
        var progress = await _db.StudentProgress.FirstOrDefaultAsync(p => p.StudentProfileId == profile.Id && p.LessonId == assessment.LessonId && p.SubjectId == assessment.SubjectId, ct);
        if (progress is null)
        {
            progress = new StudentProgress { StudentProfileId = profile.Id, LessonId = assessment.LessonId, SubjectId = assessment.SubjectId };
            _db.StudentProgress.Add(progress);
        }
        progress.CompletionPercent = Math.Max(progress.CompletionPercent, pct);
        progress.LastActivityAtUtc = DateTime.UtcNow;
        progress.Weaknesses = pct < 60 ? "Needs review / يحتاج مراجعة." : progress.Weaknesses;
        progress.Strengths = pct >= 80 ? "Good understanding / فهم جيد." : progress.Strengths;
    }

    private static AssessmentResultDto ToDto(AssessmentResult r, string title)
        => new(r.Id, r.AssessmentId, title, r.StartedAtUtc, r.SubmittedAtUtc, r.Score ?? 0, r.MaxScore ?? 0, r.Feedback);
    private static string? Null(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
