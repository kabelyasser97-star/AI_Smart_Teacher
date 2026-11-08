using AI_Smart_Teacher.Application.Curriculum;
using AI_Smart_Teacher.Core.Entities;
using AI_Smart_Teacher.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace AI_Smart_Teacher.Infrastructure.Curriculum;

public sealed class CurriculumService : ICurriculumService
{
    private readonly AppDbContext _db;
    public CurriculumService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<StageDto>> GetStagesAsync(CancellationToken ct = default)
        => await _db.EducationStages.AsNoTracking().OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .Select(x => new StageDto(x.Id, x.Name, x.Description, x.SortOrder)).ToListAsync(ct);

    public async Task<IReadOnlyList<GradeDto>> GetGradesAsync(Guid stageId, CancellationToken ct = default)
        => await _db.GradeLevels.AsNoTracking().Where(x => x.EducationStageId == stageId).OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .Select(x => new GradeDto(x.Id, x.EducationStageId, x.Name, x.SortOrder)).ToListAsync(ct);

    public async Task<IReadOnlyList<CurriculumDto>> GetCurriculaAsync(Guid gradeId, CancellationToken ct = default)
        => await _db.Curricula.AsNoTracking().Where(x => x.GradeLevelId == gradeId).OrderBy(x => x.Name)
            .Select(x => new CurriculumDto(x.Id, x.GradeLevelId, x.Name, x.Description)).ToListAsync(ct);

    public async Task<IReadOnlyList<SubjectDto>> GetSubjectsAsync(Guid curriculumId, CancellationToken ct = default)
        => await _db.Subjects.AsNoTracking().Where(x => x.CurriculumId == curriculumId).OrderBy(x => x.Name)
            .Select(x => new SubjectDto(x.Id, x.CurriculumId, x.Name, x.Description)).ToListAsync(ct);

    public async Task<IReadOnlyList<UnitDto>> GetUnitsAsync(Guid subjectId, CancellationToken ct = default)
        => await _db.Units.AsNoTracking().Where(x => x.SubjectId == subjectId).OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .Select(x => new UnitDto(x.Id, x.SubjectId, x.Name, x.SortOrder)).ToListAsync(ct);

    public async Task<IReadOnlyList<LessonDto>> GetLessonsAsync(Guid unitId, CancellationToken ct = default)
        => await _db.Lessons.AsNoTracking().Where(x => x.UnitId == unitId).OrderBy(x => x.SortOrder).ThenBy(x => x.Title)
            .Select(x => new LessonDto(x.Id, x.UnitId, x.Title, x.Summary, x.Objectives, x.SortOrder)).ToListAsync(ct);

    public async Task<LessonDetailDto?> GetLessonAsync(Guid lessonId, CancellationToken ct = default)
    {
        var lesson = await _db.Lessons.AsNoTracking().Where(x => x.Id == lessonId)
            .Select(x => new LessonDto(x.Id, x.UnitId, x.Title, x.Summary, x.Objectives, x.SortOrder)).FirstOrDefaultAsync(ct);
        if (lesson is null) return null;
        var contents = await _db.LessonContents.AsNoTracking().Where(x => x.LessonId == lessonId).OrderBy(x => x.SortOrder)
            .Select(x => new LessonContentDto(x.Id, x.LessonId, x.ContentType, x.Title, x.BodyText, x.MediaPath, x.SourceUrl, x.SortOrder)).ToListAsync(ct);
        return new LessonDetailDto(lesson, contents);
    }

    public async Task<OperationResult> AddStageAsync(string name, string? description, int sortOrder, CancellationToken ct = default)
    {
        name = (name ?? string.Empty).Trim();
        if (name.Length < 2) return OperationResult.Fail("curriculum.validation");
        var e = new EducationStage { Name = name, Description = Null(description), SortOrder = sortOrder };
        _db.EducationStages.Add(e); await _db.SaveChangesAsync(ct); return OperationResult.Ok(e.Id);
    }

    public async Task<OperationResult> AddGradeAsync(Guid stageId, string name, int sortOrder, CancellationToken ct = default)
    {
        name = (name ?? string.Empty).Trim();
        if (name.Length < 1 || !await _db.EducationStages.AnyAsync(x => x.Id == stageId, ct)) return OperationResult.Fail("curriculum.validation");
        var e = new GradeLevel { EducationStageId = stageId, Name = name, SortOrder = sortOrder };
        _db.GradeLevels.Add(e); await _db.SaveChangesAsync(ct); return OperationResult.Ok(e.Id);
    }

    public async Task<OperationResult> AddCurriculumAsync(Guid gradeId, string name, string? description, CancellationToken ct = default)
    {
        name = (name ?? string.Empty).Trim();
        if (name.Length < 2 || !await _db.GradeLevels.AnyAsync(x => x.Id == gradeId, ct)) return OperationResult.Fail("curriculum.validation");
        var e = new Core.Entities.Curriculum { GradeLevelId = gradeId, Name = name, Description = Null(description) };
        _db.Curricula.Add(e); await _db.SaveChangesAsync(ct); return OperationResult.Ok(e.Id);
    }

    public async Task<OperationResult> AddSubjectAsync(Guid curriculumId, string name, string? description, CancellationToken ct = default)
    {
        name = (name ?? string.Empty).Trim();
        if (name.Length < 2 || !await _db.Curricula.AnyAsync(x => x.Id == curriculumId, ct)) return OperationResult.Fail("curriculum.validation");
        var e = new Subject { CurriculumId = curriculumId, Name = name, Description = Null(description) };
        _db.Subjects.Add(e); await _db.SaveChangesAsync(ct); return OperationResult.Ok(e.Id);
    }

    public async Task<OperationResult> AddUnitAsync(Guid subjectId, string name, int sortOrder, CancellationToken ct = default)
    {
        name = (name ?? string.Empty).Trim();
        if (name.Length < 1 || !await _db.Subjects.AnyAsync(x => x.Id == subjectId, ct)) return OperationResult.Fail("curriculum.validation");
        var e = new Unit { SubjectId = subjectId, Name = name, SortOrder = sortOrder };
        _db.Units.Add(e); await _db.SaveChangesAsync(ct); return OperationResult.Ok(e.Id);
    }

    public async Task<OperationResult> AddLessonAsync(Guid unitId, string title, string? summary, string? objectives, int sortOrder, CancellationToken ct = default)
    {
        title = (title ?? string.Empty).Trim();
        if (title.Length < 2 || !await _db.Units.AnyAsync(x => x.Id == unitId, ct)) return OperationResult.Fail("curriculum.validation");
        var e = new Lesson { UnitId = unitId, Title = title, Summary = Null(summary), Objectives = Null(objectives), SortOrder = sortOrder };
        _db.Lessons.Add(e); await _db.SaveChangesAsync(ct); return OperationResult.Ok(e.Id);
    }

    public async Task<OperationResult> AddLessonContentAsync(Guid lessonId, Core.Enums.ContentType type, string title, string? body, string? mediaPath, string? sourceUrl, int sortOrder, CancellationToken ct = default)
    {
        title = (title ?? string.Empty).Trim();
        if (title.Length < 2 || !await _db.Lessons.AnyAsync(x => x.Id == lessonId, ct)) return OperationResult.Fail("curriculum.validation");
        var e = new LessonContent { LessonId = lessonId, ContentType = type, Title = title, BodyText = Null(body), MediaPath = Null(mediaPath), SourceUrl = Null(sourceUrl), SortOrder = sortOrder };
        _db.LessonContents.Add(e); await _db.SaveChangesAsync(ct); return OperationResult.Ok(e.Id);
    }

    private static string? Null(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
