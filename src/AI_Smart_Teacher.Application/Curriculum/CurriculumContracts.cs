using AI_Smart_Teacher.Core.Enums;

namespace AI_Smart_Teacher.Application.Curriculum;

public sealed record CurriculumNode(Guid Id, string Name, string Kind, Guid? ParentId, int SortOrder);
public sealed record StageDto(Guid Id, string Name, string? Description, int SortOrder);
public sealed record GradeDto(Guid Id, Guid EducationStageId, string Name, int SortOrder);
public sealed record CurriculumDto(Guid Id, Guid GradeLevelId, string Name, string? Description);
public sealed record SubjectDto(Guid Id, Guid CurriculumId, string Name, string? Description);
public sealed record UnitDto(Guid Id, Guid SubjectId, string Name, int SortOrder);
public sealed record LessonDto(Guid Id, Guid UnitId, string Title, string? Summary, string? Objectives, int SortOrder);
public sealed record LessonContentDto(Guid Id, Guid LessonId, ContentType ContentType, string Title, string? BodyText, string? MediaPath, string? SourceUrl, int SortOrder);
public sealed record LessonDetailDto(LessonDto Lesson, IReadOnlyList<LessonContentDto> Contents);

public sealed record OperationResult(bool Success, string? ErrorCode = null, Guid? Id = null)
{
    public static OperationResult Ok(Guid? id = null) => new(true, null, id);
    public static OperationResult Fail(string code) => new(false, code, null);
}

public interface ICurriculumService
{
    Task<IReadOnlyList<StageDto>> GetStagesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<GradeDto>> GetGradesAsync(Guid stageId, CancellationToken ct = default);
    Task<IReadOnlyList<CurriculumDto>> GetCurriculaAsync(Guid gradeId, CancellationToken ct = default);
    Task<IReadOnlyList<SubjectDto>> GetSubjectsAsync(Guid curriculumId, CancellationToken ct = default);
    Task<IReadOnlyList<UnitDto>> GetUnitsAsync(Guid subjectId, CancellationToken ct = default);
    Task<IReadOnlyList<LessonDto>> GetLessonsAsync(Guid unitId, CancellationToken ct = default);
    Task<LessonDetailDto?> GetLessonAsync(Guid lessonId, CancellationToken ct = default);

    Task<OperationResult> AddStageAsync(string name, string? description, int sortOrder, CancellationToken ct = default);
    Task<OperationResult> AddGradeAsync(Guid stageId, string name, int sortOrder, CancellationToken ct = default);
    Task<OperationResult> AddCurriculumAsync(Guid gradeId, string name, string? description, CancellationToken ct = default);
    Task<OperationResult> AddSubjectAsync(Guid curriculumId, string name, string? description, CancellationToken ct = default);
    Task<OperationResult> AddUnitAsync(Guid subjectId, string name, int sortOrder, CancellationToken ct = default);
    Task<OperationResult> AddLessonAsync(Guid unitId, string title, string? summary, string? objectives, int sortOrder, CancellationToken ct = default);
    Task<OperationResult> AddLessonContentAsync(Guid lessonId, ContentType type, string title, string? body, string? mediaPath, string? sourceUrl, int sortOrder, CancellationToken ct = default);
}
