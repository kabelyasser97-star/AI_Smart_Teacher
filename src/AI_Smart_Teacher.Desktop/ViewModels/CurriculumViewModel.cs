using AI_Smart_Teacher.Application.Auth;
using AI_Smart_Teacher.Application.Curriculum;
using AI_Smart_Teacher.Core.Enums;
using AI_Smart_Teacher.Core.Security;
using AI_Smart_Teacher.Desktop.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AI_Smart_Teacher.Desktop.ViewModels;

public sealed class CurriculumViewModel
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICurrentUserSession _session;
    public CurriculumViewModel(IServiceScopeFactory scopeFactory, ICurrentUserSession session)
    {
        _scopeFactory = scopeFactory;
        _session = session;
    }

    public string Message { get; private set; } = string.Empty;
    public bool IsError { get; private set; }
    public bool CanManage => _session.HasPermission(AppPermissions.ManageCurriculum);
    public IReadOnlyList<StageDto> Stages { get; private set; } = Array.Empty<StageDto>();
    public IReadOnlyList<GradeDto> Grades { get; private set; } = Array.Empty<GradeDto>();
    public IReadOnlyList<CurriculumDto> Curricula { get; private set; } = Array.Empty<CurriculumDto>();
    public IReadOnlyList<SubjectDto> Subjects { get; private set; } = Array.Empty<SubjectDto>();
    public IReadOnlyList<UnitDto> Units { get; private set; } = Array.Empty<UnitDto>();
    public IReadOnlyList<LessonDto> Lessons { get; private set; } = Array.Empty<LessonDto>();
    public LessonDetailDto? SelectedLesson { get; private set; }

    public IReadOnlyCollection<string> PermissionsFromSession() => _session.User is null ? Array.Empty<string>() : AppPermissions.ForRole(_session.User.Role);

    public async Task LoadStagesAsync(CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<ICurriculumService>();
        Stages = await svc.GetStagesAsync(ct);
        Grades = Array.Empty<GradeDto>(); Curricula = Array.Empty<CurriculumDto>(); Subjects = Array.Empty<SubjectDto>(); Units = Array.Empty<UnitDto>(); Lessons = Array.Empty<LessonDto>(); SelectedLesson = null;
    }

    public async Task LoadGradesAsync(Guid stageId, CancellationToken ct = default) { using var s=_scopeFactory.CreateScope(); var svc=s.ServiceProvider.GetRequiredService<ICurriculumService>(); Grades=await svc.GetGradesAsync(stageId,ct); Curricula=Array.Empty<CurriculumDto>(); Subjects=Array.Empty<SubjectDto>(); Units=Array.Empty<UnitDto>(); Lessons=Array.Empty<LessonDto>(); SelectedLesson=null; }
    public async Task LoadCurriculaAsync(Guid gradeId, CancellationToken ct = default) { using var s=_scopeFactory.CreateScope(); var svc=s.ServiceProvider.GetRequiredService<ICurriculumService>(); Curricula=await svc.GetCurriculaAsync(gradeId,ct); Subjects=Array.Empty<SubjectDto>(); Units=Array.Empty<UnitDto>(); Lessons=Array.Empty<LessonDto>(); SelectedLesson=null; }
    public async Task LoadSubjectsAsync(Guid curriculumId, CancellationToken ct = default) { using var s=_scopeFactory.CreateScope(); var svc=s.ServiceProvider.GetRequiredService<ICurriculumService>(); Subjects=await svc.GetSubjectsAsync(curriculumId,ct); Units=Array.Empty<UnitDto>(); Lessons=Array.Empty<LessonDto>(); SelectedLesson=null; }
    public async Task LoadUnitsAsync(Guid subjectId, CancellationToken ct = default) { using var s=_scopeFactory.CreateScope(); var svc=s.ServiceProvider.GetRequiredService<ICurriculumService>(); Units=await svc.GetUnitsAsync(subjectId,ct); Lessons=Array.Empty<LessonDto>(); SelectedLesson=null; }
    public async Task LoadLessonsAsync(Guid unitId, CancellationToken ct = default) { using var s=_scopeFactory.CreateScope(); var svc=s.ServiceProvider.GetRequiredService<ICurriculumService>(); Lessons=await svc.GetLessonsAsync(unitId,ct); SelectedLesson=null; }
    public async Task LoadLessonAsync(Guid lessonId, CancellationToken ct = default) { using var s=_scopeFactory.CreateScope(); var svc=s.ServiceProvider.GetRequiredService<ICurriculumService>(); SelectedLesson=await svc.GetLessonAsync(lessonId,ct); }

    public Task<bool> AddStageAsync(string name, string? desc) => Manage(async svc => (await svc.AddStageAsync(name, desc, Stages.Count + 1)).Success);
    public Task<bool> AddGradeAsync(Guid stageId, string name) => Manage(async svc => (await svc.AddGradeAsync(stageId, name, Grades.Count + 1)).Success);
    public Task<bool> AddCurriculumAsync(Guid gradeId, string name, string? desc) => Manage(async svc => (await svc.AddCurriculumAsync(gradeId, name, desc)).Success);
    public Task<bool> AddSubjectAsync(Guid curriculumId, string name, string? desc) => Manage(async svc => (await svc.AddSubjectAsync(curriculumId, name, desc)).Success);
    public Task<bool> AddUnitAsync(Guid subjectId, string name) => Manage(async svc => (await svc.AddUnitAsync(subjectId, name, Units.Count + 1)).Success);
    public Task<bool> AddLessonAsync(Guid unitId, string title, string? summary, string? objectives) => Manage(async svc => (await svc.AddLessonAsync(unitId, title, summary, objectives, Lessons.Count + 1)).Success);
    public Task<bool> AddLessonContentAsync(Guid lessonId, ContentType type, string title, string? body, string? path, string? url) => Manage(async svc => (await svc.AddLessonContentAsync(lessonId, type, title, body, path, url, (SelectedLesson?.Contents.Count ?? 0) + 1)).Success);

    private async Task<bool> Manage(Func<ICurriculumService, Task<bool>> action)
    {
        if (!CanManage) { SetMessage("curriculum.denied", true); return false; }
        using var scope = _scopeFactory.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<ICurriculumService>();
        var ok = await action(svc);
        SetMessage(ok ? "curriculum.saved" : "curriculum.validation", !ok);
        return ok;
    }

    private void SetMessage(string key, bool error)
    {
        var ar = LocalizationService.Current == UiLanguage.Arabic;
        IsError = error;
        Message = key switch
        {
            "curriculum.saved" => ar ? "تم حفظ العنصر." : "Item saved.",
            "curriculum.denied" => ar ? "ليست لديك صلاحية إدارة المناهج." : "You do not have curriculum management permission.",
            "curriculum.validation" => ar ? "تحقق من البيانات أو اختر العنصر الأب أولاً." : "Check the input or select the parent item first.",
            _ => ar ? "حدث خطأ." : "An error occurred."
        };
    }
}
