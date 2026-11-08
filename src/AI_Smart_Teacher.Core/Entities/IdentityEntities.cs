using AI_Smart_Teacher.Core.Enums;

namespace AI_Smart_Teacher.Core.Entities;

public class Role : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public UserRoleType Type { get; set; }
    public string? Description { get; set; }
    public ICollection<User> Users { get; set; } = new List<User>();
}

public class User : EntityBase
{
    public string Username { get; set; } = string.Empty;
    public string NormalizedUsername { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? NormalizedEmail { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PreferredLanguage { get; set; } = "ar";
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAtUtc { get; set; }
    public Guid RoleId { get; set; }
    public Role? Role { get; set; }
    public StudentProfile? StudentProfile { get; set; }
    public ParentProfile? ParentProfile { get; set; }
    public ICollection<ParentStudentLink> ParentLinks { get; set; } = new List<ParentStudentLink>();
    public ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
    public ICollection<AssessmentResult> AssessmentResults { get; set; } = new List<AssessmentResult>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}

public class StudentProfile : EntityBase
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public Guid? GradeLevelId { get; set; }
    public GradeLevel? GradeLevel { get; set; }
    public string? Notes { get; set; }
    public ICollection<ParentStudentLink> ParentLinks { get; set; } = new List<ParentStudentLink>();
    public ICollection<StudentProgress> ProgressRecords { get; set; } = new List<StudentProgress>();
}

public class ParentProfile : EntityBase
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string? PhoneNumber { get; set; }
    public ICollection<ParentStudentLink> StudentLinks { get; set; } = new List<ParentStudentLink>();
}

public class ParentStudentLink : EntityBase
{
    public Guid ParentProfileId { get; set; }
    public ParentProfile? ParentProfile { get; set; }
    public Guid StudentProfileId { get; set; }
    public StudentProfile? StudentProfile { get; set; }
    public string Relationship { get; set; } = "Parent";
    public bool CanViewProgress { get; set; } = true;
}
