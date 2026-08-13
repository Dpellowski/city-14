using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database._Misfits.Experience;

[Table("character_experience")]
public sealed class CharacterExperienceModel
{
    public int ProfileId { get; set; }

    public Profile Profile { get; set; } = null!;

    [MaxLength(64)]
    public string ExperienceGroup { get; set; } = string.Empty;

    public long TotalExperience { get; set; }
}

public static class CharacterExperienceDatabaseModel
{
    public static void Configure(ModelBuilder modelBuilder)
    {
        var experience = modelBuilder.Entity<CharacterExperienceModel>();
        experience.HasKey(e => new { e.ProfileId, e.ExperienceGroup });
        experience.HasOne(e => e.Profile)
            .WithMany()
            .HasForeignKey(e => e.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);
        experience.ToTable(table => table.HasCheckConstraint(
            "TotalExperienceNonNegative",
            "total_experience >= 0"));
    }
}
