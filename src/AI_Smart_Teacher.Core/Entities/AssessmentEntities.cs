using AI_Smart_Teacher.Core.Enums;

namespace AI_Smart_Teacher.Core.Entities;

public class Assessment : EntityBase
{
    public Guid? LessonId { get; set; }
    public Lesson? Lesson { get; set; }
    public Guid? SubjectId { get; set; }
    public Subject? Subject { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }
    public bool IsPublished { get; set; }
    public ICollection<AssessmentQuestion> Questions { get; set; } = new List<AssessmentQuestion>();
    public ICollection<AssessmentResult> Results { get; set; } = new List<AssessmentResult>();
}

public class AssessmentQuestion : EntityBase
{
    public Guid AssessmentId { get; set; }
    public Assessment? Assessment { get; set; }
    public QuestionType QuestionType { get; set; }
    public string Prompt { get; set; } = string.Empty;
    public string? Explanation { get; set; }
    public decimal Points { get; set; } = 1;
    public int SortOrder { get; set; }
    public ICollection<QuestionOption> Options { get; set; } = new List<QuestionOption>();
    public ICollection<StudentAnswer> StudentAnswers { get; set; } = new List<StudentAnswer>();
}

public class QuestionOption : EntityBase
{
    public Guid QuestionId { get; set; }
    public AssessmentQuestion? Question { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int SortOrder { get; set; }
}

public class AssessmentResult : EntityBase
{
    public Guid AssessmentId { get; set; }
    public Assessment? Assessment { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
    public decimal? Score { get; set; }
    public decimal? MaxScore { get; set; }
    public string? Feedback { get; set; }
    public ICollection<StudentAnswer> Answers { get; set; } = new List<StudentAnswer>();
}

public class StudentAnswer : EntityBase
{
    public Guid AssessmentResultId { get; set; }
    public AssessmentResult? AssessmentResult { get; set; }
    public Guid QuestionId { get; set; }
    public AssessmentQuestion? Question { get; set; }
    public string? AnswerText { get; set; }
    public Guid? SelectedOptionId { get; set; }
    public QuestionOption? SelectedOption { get; set; }
    public bool? IsCorrect { get; set; }
    public decimal? PointsAwarded { get; set; }
    public string? Feedback { get; set; }
}

public class StudentProgress : EntityBase
{
    public Guid StudentProfileId { get; set; }
    public StudentProfile? StudentProfile { get; set; }
    public Guid? LessonId { get; set; }
    public Lesson? Lesson { get; set; }
    public Guid? SubjectId { get; set; }
    public Subject? Subject { get; set; }
    public int CompletionPercent { get; set; }
    public string? Strengths { get; set; }
    public string? Weaknesses { get; set; }
    public DateTime LastActivityAtUtc { get; set; } = DateTime.UtcNow;
}
