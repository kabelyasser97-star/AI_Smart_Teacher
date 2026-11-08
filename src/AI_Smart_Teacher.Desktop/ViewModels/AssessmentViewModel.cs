using AI_Smart_Teacher.Application.Assessments;
using AI_Smart_Teacher.Application.Auth;
using AI_Smart_Teacher.Application.Curriculum;
using AI_Smart_Teacher.Core.Enums;
using AI_Smart_Teacher.Core.Security;
using Microsoft.Extensions.DependencyInjection;

namespace AI_Smart_Teacher.Desktop.ViewModels;

public sealed class AssessmentViewModel
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICurrentUserSession _session;
    public AssessmentViewModel(IServiceScopeFactory scopeFactory, ICurrentUserSession session) { _scopeFactory = scopeFactory; _session = session; }

    public string Status { get; private set; } = string.Empty;
    public bool IsError { get; private set; }
    public bool CanManage => _session.HasPermission(AppPermissions.ManageAssessments);
    public bool CanTake => _session.HasPermission(AppPermissions.TakeAssessments);
    public bool CanViewReports => _session.HasPermission(AppPermissions.ViewAllReports);
    public bool CanViewOwnProgress => _session.HasPermission(AppPermissions.ViewOwnProgress);
    public IReadOnlyList<AssessmentSummaryDto> Assessments { get; private set; } = Array.Empty<AssessmentSummaryDto>();
    public AssessmentDetailDto? Selected { get; private set; }
    public IReadOnlyList<AssessmentResultDto> Results { get; private set; } = Array.Empty<AssessmentResultDto>();
    public IReadOnlyList<StudentProgressDto> Progress { get; private set; } = Array.Empty<StudentProgressDto>();

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IAssessmentService>();
        Assessments = await svc.ListAsync(ct);
        if (_session.User is null) { Results = Array.Empty<AssessmentResultDto>(); Progress = Array.Empty<StudentProgressDto>(); return; }
        if (CanViewReports) Results = await svc.GetAllResultsAsync(ct);
        else if (CanTake || CanViewOwnProgress) Results = await svc.GetMyResultsAsync(_session.User.Id, ct);
        else Results = Array.Empty<AssessmentResultDto>();
        if (CanViewOwnProgress) Progress = await svc.GetMyProgressAsync(_session.User.Id, ct);
    }

    public async Task LoadAssessmentAsync(Guid id, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        Selected = await scope.ServiceProvider.GetRequiredService<IAssessmentService>().GetAsync(id, CanManage, ct);
    }

    public async Task CreateAsync(string title, string? description, int duration, CancellationToken ct = default)
    {
        if (!CanManage) { Fail("Permission denied."); return; }
        using var scope = _scopeFactory.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<IAssessmentService>().CreateAssessmentAsync(title, description, null, null, duration, ct);
        Status = result.Success ? "Assessment created." : (result.ErrorCode ?? "Failed."); IsError = !result.Success;
        await RefreshAsync(ct);
    }

    public async Task AddMultipleChoiceAsync(Guid assessmentId, string prompt, decimal points, string optionsLines, CancellationToken ct = default)
    {
        if (!CanManage) { Fail("Permission denied."); return; }
        var options = (optionsLines ?? string.Empty).Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim()).Where(l => l.Length > 0)
            .Select(l => l.StartsWith("*") ? (Text: l.TrimStart('*', ' ').Trim(), IsCorrect: true) : (Text: l, IsCorrect: false)).ToList();
        using var scope = _scopeFactory.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<IAssessmentService>().AddQuestionAsync(assessmentId, QuestionType.MultipleChoice, prompt, points, null, options, ct);
        Status = result.Success ? "Question added." : (result.ErrorCode ?? "Failed."); IsError = !result.Success;
        await LoadAssessmentAsync(assessmentId, ct);
    }

    public async Task PublishAsync(Guid assessmentId, bool published, CancellationToken ct = default)
    {
        if (!CanManage) { Fail("Permission denied."); return; }
        using var scope = _scopeFactory.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<IAssessmentService>().SetPublishedAsync(assessmentId, published, ct);
        Status = result.Success ? (published ? "Published." : "Unpublished.") : (result.ErrorCode ?? "Failed."); IsError = !result.Success;
        await RefreshAsync(ct);
        await LoadAssessmentAsync(assessmentId, ct);
    }

    public async Task SubmitCurrentAsync(IEnumerable<Guid> selectedOptionIds, CancellationToken ct = default)
    {
        if (_session.User is null || Selected is null) { Fail("Sign in and select an assessment."); return; }
        if (!CanTake) { Fail("Permission denied."); return; }
        var ids = selectedOptionIds?.ToHashSet() ?? new HashSet<Guid>();
        var answers = Selected.Questions.Select(q => new AssessmentAnswerInput(q.Id, q.Options.FirstOrDefault(o => ids.Contains(o.Id))?.Id, null)).ToList();
        using var scope = _scopeFactory.CreateScope();
        var (result, score) = await scope.ServiceProvider.GetRequiredService<IAssessmentService>().SubmitAsync(new SubmitAssessmentRequest(_session.User.Id, Selected.Assessment.Id, answers), ct);
        Status = result.Success && score is not null ? $"Score: {score.Score}/{score.MaxScore}. {score.Feedback}" : (result.ErrorCode ?? "Submit failed.");
        IsError = !result.Success;
        await RefreshAsync(ct);
    }

    public Task SubmitCurrentAsync(string selectedOptionIdsCsv, CancellationToken ct = default)
    {
        var ids = (selectedOptionIdsCsv ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => Guid.TryParse(s, out var id) ? id : (Guid?)null).Where(id => id.HasValue).Select(id => id!.Value);
        return SubmitCurrentAsync(ids, ct);
    }

    private void Fail(string message) { Status = message; IsError = true; }
}
