namespace AI_Smart_Teacher.Core.Security;

public static class AppPermissions
{
    public const string ManageUsers = "users.manage";
    public const string ViewUsers = "users.view";
    public const string ManageCurriculum = "curriculum.manage";
    public const string ViewCurriculum = "curriculum.view";
    public const string UseAiTutor = "ai.use";
    public const string ManageAiSettings = "ai.settings.manage";
    public const string ViewOwnProgress = "progress.view.own";
    public const string ViewLinkedStudentProgress = "progress.view.linked";
    public const string ViewAllReports = "reports.view.all";
    public const string ManageAssessments = "assessments.manage";
    public const string TakeAssessments = "assessments.take";
    public const string ManageSubscriptions = "subscriptions.manage";
    public const string ManageSystemSettings = "system.settings.manage";

    public static IReadOnlyCollection<string> ForRole(Core.Enums.UserRoleType role) => role switch
    {
        Core.Enums.UserRoleType.Admin => new[]
        {
            ManageUsers, ViewUsers, ManageCurriculum, ViewCurriculum, UseAiTutor, ManageAiSettings,
            ViewAllReports, ManageAssessments, ManageSubscriptions, ManageSystemSettings
        },
        Core.Enums.UserRoleType.Teacher => new[] { ViewUsers, ManageCurriculum, ViewCurriculum, UseAiTutor, ManageAssessments, ViewAllReports },
        Core.Enums.UserRoleType.Parent => new[] { ViewCurriculum, ViewLinkedStudentProgress },
        Core.Enums.UserRoleType.Student => new[] { ViewCurriculum, UseAiTutor, ViewOwnProgress, TakeAssessments },
        _ => Array.Empty<string>()
    };
}
