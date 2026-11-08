using AI_Smart_Teacher.Core.Enums;

namespace AI_Smart_Teacher.Core.Entities;

public class EducationStage : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public ICollection<GradeLevel> GradeLevels { get; set; } = new List<GradeLevel>();
}

public class GradeLevel : EntityBase
{
    public Guid EducationStageId { get; set; }
    public EducationStage? EducationStage { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public ICollection<Curriculum> Curricula { get; set; } = new List<Curriculum>();
    public ICollection<StudentProfile> Students { get; set; } = new List<StudentProfile>();
}

public class Curriculum : EntityBase
{
    public Guid GradeLevelId { get; set; }
    public GradeLevel? GradeLevel { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ICollection<Subject> Subjects { get; set; } = new List<Subject>();
}

public class Subject : EntityBase
{
    public Guid CurriculumId { get; set; }
    public Curriculum? Curriculum { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ICollection<Unit> Units { get; set; } = new List<Unit>();
}

public class Unit : EntityBase
{
    public Guid SubjectId { get; set; }
    public Subject? Subject { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
}

public class Lesson : EntityBase
{
    public Guid UnitId { get; set; }
    public Unit? Unit { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? Objectives { get; set; }
    public int SortOrder { get; set; }
    public ICollection<LessonContent> Contents { get; set; } = new List<LessonContent>();
    public ICollection<Assessment> Assessments { get; set; } = new List<Assessment>();
}

public class LessonContent : EntityBase
{
    public Guid LessonId { get; set; }
    public Lesson? Lesson { get; set; }
    public ContentType ContentType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? BodyText { get; set; }
    public string? MediaPath { get; set; }
    public string? SourceUrl { get; set; }
    public int SortOrder { get; set; }
}
