using AI_Smart_Teacher.Core.Entities;
using AI_Smart_Teacher.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AI_Smart_Teacher.Data.Configurations;

internal class IdentityConfiguration : IEntityTypeConfiguration<Role>, IEntityTypeConfiguration<User>,
    IEntityTypeConfiguration<StudentProfile>, IEntityTypeConfiguration<ParentProfile>, IEntityTypeConfiguration<ParentStudentLink>
{
    public void Configure(EntityTypeBuilder<Role> b)
    {
        b.HasIndex(x => x.Name).IsUnique();
        b.Property(x => x.Name).HasMaxLength(64).IsRequired();
    }

    public void Configure(EntityTypeBuilder<User> b)
    {
        b.HasIndex(x => x.NormalizedUsername).IsUnique();
        b.HasIndex(x => x.NormalizedEmail);
        b.Property(x => x.Username).HasMaxLength(64).IsRequired();
        b.Property(x => x.NormalizedUsername).HasMaxLength(64).IsRequired();
        b.Property(x => x.Email).HasMaxLength(256);
        b.Property(x => x.NormalizedEmail).HasMaxLength(256);
        b.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
        b.Property(x => x.PasswordSalt).HasMaxLength(256).IsRequired();
        b.Property(x => x.DisplayName).HasMaxLength(128).IsRequired();
        b.Property(x => x.PreferredLanguage).HasMaxLength(16).HasDefaultValue("ar");
        b.HasOne(x => x.Role).WithMany(x => x.Users).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<StudentProfile> b)
    {
        b.HasIndex(x => x.UserId).IsUnique();
        b.HasOne(x => x.User).WithOne(x => x.StudentProfile).HasForeignKey<StudentProfile>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.GradeLevel).WithMany(x => x.Students).HasForeignKey(x => x.GradeLevelId).OnDelete(DeleteBehavior.SetNull);
    }

    public void Configure(EntityTypeBuilder<ParentProfile> b)
    {
        b.HasIndex(x => x.UserId).IsUnique();
        b.Property(x => x.PhoneNumber).HasMaxLength(32);
        b.HasOne(x => x.User).WithOne(x => x.ParentProfile).HasForeignKey<ParentProfile>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }

    public void Configure(EntityTypeBuilder<ParentStudentLink> b)
    {
        b.HasIndex(x => new { x.ParentProfileId, x.StudentProfileId }).IsUnique();
        b.Property(x => x.Relationship).HasMaxLength(64).HasDefaultValue("Parent");
        b.HasOne(x => x.ParentProfile).WithMany(x => x.StudentLinks).HasForeignKey(x => x.ParentProfileId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.StudentProfile).WithMany(x => x.ParentLinks).HasForeignKey(x => x.StudentProfileId).OnDelete(DeleteBehavior.Cascade);
    }
}
