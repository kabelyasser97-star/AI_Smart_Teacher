using AI_Smart_Teacher.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AI_Smart_Teacher.Data.Configurations;

internal class CurriculumConfiguration : IEntityTypeConfiguration<EducationStage>, IEntityTypeConfiguration<GradeLevel>,
    IEntityTypeConfiguration<Curriculum>, IEntityTypeConfiguration<Subject>, IEntityTypeConfiguration<Unit>,
    IEntityTypeConfiguration<Lesson>, IEntityTypeConfiguration<LessonContent>
{
    public void Configure(EntityTypeBuilder<EducationStage> b) { b.Property(x => x.Name).HasMaxLength(128).IsRequired(); }
    public void Configure(EntityTypeBuilder<GradeLevel> b)
    {
        b.Property(x => x.Name).HasMaxLength(128).IsRequired();
        b.HasOne(x => x.EducationStage).WithMany(x => x.GradeLevels).HasForeignKey(x => x.EducationStageId).OnDelete(DeleteBehavior.Cascade);
    }
    public void Configure(EntityTypeBuilder<Curriculum> b)
    {
        b.Property(x => x.Name).HasMaxLength(128).IsRequired();
        b.HasOne(x => x.GradeLevel).WithMany(x => x.Curricula).HasForeignKey(x => x.GradeLevelId).OnDelete(DeleteBehavior.Cascade);
    }
    public void Configure(EntityTypeBuilder<Subject> b)
    {
        b.Property(x => x.Name).HasMaxLength(128).IsRequired();
        b.HasOne(x => x.Curriculum).WithMany(x => x.Subjects).HasForeignKey(x => x.CurriculumId).OnDelete(DeleteBehavior.Cascade);
    }
    public void Configure(EntityTypeBuilder<Unit> b)
    {
        b.Property(x => x.Name).HasMaxLength(128).IsRequired();
        b.HasOne(x => x.Subject).WithMany(x => x.Units).HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.Cascade);
    }
    public void Configure(EntityTypeBuilder<Lesson> b)
    {
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.HasOne(x => x.Unit).WithMany(x => x.Lessons).HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Cascade);
    }
    public void Configure(EntityTypeBuilder<LessonContent> b)
    {
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.MediaPath).HasMaxLength(1024);
        b.Property(x => x.SourceUrl).HasMaxLength(2048);
        b.HasOne(x => x.Lesson).WithMany(x => x.Contents).HasForeignKey(x => x.LessonId).OnDelete(DeleteBehavior.Cascade);
    }
}
