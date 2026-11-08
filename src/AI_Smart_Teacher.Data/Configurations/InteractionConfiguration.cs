using AI_Smart_Teacher.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AI_Smart_Teacher.Data.Configurations;

internal class InteractionConfiguration : IEntityTypeConfiguration<Conversation>, IEntityTypeConfiguration<ChatMessage>,
    IEntityTypeConfiguration<Attachment>, IEntityTypeConfiguration<Assessment>, IEntityTypeConfiguration<AssessmentQuestion>,
    IEntityTypeConfiguration<QuestionOption>, IEntityTypeConfiguration<AssessmentResult>, IEntityTypeConfiguration<StudentAnswer>,
    IEntityTypeConfiguration<StudentProgress>, IEntityTypeConfiguration<SystemSetting>, IEntityTypeConfiguration<AuditLog>,
    IEntityTypeConfiguration<SubscriptionPlan>, IEntityTypeConfiguration<Subscription>, IEntityTypeConfiguration<PaymentRecord>,
    IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Conversation> b)
    {
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.ProviderName).HasMaxLength(64).HasDefaultValue("NotConfigured");
        b.HasOne(x => x.User).WithMany(x => x.Conversations).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Lesson).WithMany().HasForeignKey(x => x.LessonId).OnDelete(DeleteBehavior.SetNull);
    }
    public void Configure(EntityTypeBuilder<ChatMessage> b)
    {
        b.Property(x => x.Role).HasMaxLength(32).IsRequired();
        b.Property(x => x.ModelName).HasMaxLength(128);
        b.HasOne(x => x.Conversation).WithMany(x => x.Messages).HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Cascade);
    }
    public void Configure(EntityTypeBuilder<Attachment> b)
    {
        b.Property(x => x.FileName).HasMaxLength(255).IsRequired();
        b.Property(x => x.ContentType).HasMaxLength(128);
        b.Property(x => x.StoragePath).HasMaxLength(1024).IsRequired();
        b.HasOne(x => x.ChatMessage).WithMany(x => x.Attachments).HasForeignKey(x => x.ChatMessageId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.LessonContent).WithMany().HasForeignKey(x => x.LessonContentId).OnDelete(DeleteBehavior.SetNull);
    }
    public void Configure(EntityTypeBuilder<Assessment> b)
    {
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.HasOne(x => x.Lesson).WithMany(x => x.Assessments).HasForeignKey(x => x.LessonId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.Subject).WithMany().HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.SetNull);
    }
    public void Configure(EntityTypeBuilder<AssessmentQuestion> b)
    {
        b.Property(x => x.Points).HasPrecision(10, 2);
        b.HasOne(x => x.Assessment).WithMany(x => x.Questions).HasForeignKey(x => x.AssessmentId).OnDelete(DeleteBehavior.Cascade);
    }
    public void Configure(EntityTypeBuilder<QuestionOption> b)
    {
        b.Property(x => x.Text).HasMaxLength(1000).IsRequired();
        b.HasOne(x => x.Question).WithMany(x => x.Options).HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Cascade);
    }
    public void Configure(EntityTypeBuilder<AssessmentResult> b)
    {
        b.Property(x => x.Score).HasPrecision(10, 2);
        b.Property(x => x.MaxScore).HasPrecision(10, 2);
        b.HasOne(x => x.Assessment).WithMany(x => x.Results).HasForeignKey(x => x.AssessmentId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.User).WithMany(x => x.AssessmentResults).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
    public void Configure(EntityTypeBuilder<StudentAnswer> b)
    {
        b.Property(x => x.PointsAwarded).HasPrecision(10, 2);
        b.HasOne(x => x.AssessmentResult).WithMany(x => x.Answers).HasForeignKey(x => x.AssessmentResultId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Question).WithMany(x => x.StudentAnswers).HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.SelectedOption).WithMany().HasForeignKey(x => x.SelectedOptionId).OnDelete(DeleteBehavior.SetNull);
    }
    public void Configure(EntityTypeBuilder<StudentProgress> b)
    {
        b.HasIndex(x => new { x.StudentProfileId, x.LessonId, x.SubjectId });
        b.HasOne(x => x.StudentProfile).WithMany(x => x.ProgressRecords).HasForeignKey(x => x.StudentProfileId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Lesson).WithMany().HasForeignKey(x => x.LessonId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.Subject).WithMany().HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.SetNull);
    }
    public void Configure(EntityTypeBuilder<SystemSetting> b)
    {
        b.HasIndex(x => x.Key).IsUnique();
        b.Property(x => x.Key).HasMaxLength(128).IsRequired();
        b.Property(x => x.Category).HasMaxLength(64);
    }
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.Property(x => x.Action).HasMaxLength(128).IsRequired();
        b.Property(x => x.EntityName).HasMaxLength(128);
        b.Property(x => x.EntityId).HasMaxLength(128);
        b.HasIndex(x => x.CreatedAtUtc);
        b.HasOne(x => x.User).WithMany(x => x.AuditLogs).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
    }
    public void Configure(EntityTypeBuilder<SubscriptionPlan> b)
    {
        b.Property(x => x.Name).HasMaxLength(128).IsRequired();
        b.Property(x => x.Currency).HasMaxLength(8).HasDefaultValue("USD");
        b.Property(x => x.Price).HasPrecision(18, 2);
    }
    public void Configure(EntityTypeBuilder<Subscription> b)
    {
        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Plan).WithMany().HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.Restrict);
    }
    public void Configure(EntityTypeBuilder<PaymentRecord> b)
    {
        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.Property(x => x.Currency).HasMaxLength(8).HasDefaultValue("USD");
        b.Property(x => x.Provider).HasMaxLength(64).HasDefaultValue("Manual");
        b.Property(x => x.Status).HasMaxLength(32).HasDefaultValue("Pending");
        b.Property(x => x.ExternalReference).HasMaxLength(128);
        b.HasOne(x => x.Subscription).WithMany().HasForeignKey(x => x.SubscriptionId).OnDelete(DeleteBehavior.Cascade);
    }
    public void Configure(EntityTypeBuilder<Notification> b)
    {
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
