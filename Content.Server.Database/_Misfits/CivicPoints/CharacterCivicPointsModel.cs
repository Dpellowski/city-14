using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database._Misfits.CivicPoints;

[Table("character_civic_points")]
public sealed class CharacterCivicPointsModel
{
    public const long StartingPoints = 50;

    public int ProfileId { get; set; }

    public Profile Profile { get; set; } = null!;

    public long Points { get; set; } = StartingPoints;
}

[Table("character_civic_point_change")]
public sealed class CharacterCivicPointChangeModel
{
    public long Id { get; set; }

    public int ProfileId { get; set; }

    public Profile Profile { get; set; } = null!;

    public long Delta { get; set; }

    public long BalanceAfter { get; set; }

    [MaxLength(32)]
    public string SourceKind { get; set; } = string.Empty;

    [MaxLength(128)]
    public string SourceId { get; set; } = string.Empty;

    [MaxLength(512)]
    public string Reason { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}

public static class CivicPointsDatabaseModel
{
    public static void Configure(ModelBuilder modelBuilder)
    {
        var balance = modelBuilder.Entity<CharacterCivicPointsModel>();
        balance.HasKey(e => e.ProfileId);
        balance.Property(e => e.Points).HasDefaultValue(CharacterCivicPointsModel.StartingPoints);
        balance.HasOne(e => e.Profile)
            .WithOne()
            .HasForeignKey<CharacterCivicPointsModel>(e => e.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        var change = modelBuilder.Entity<CharacterCivicPointChangeModel>();
        change.HasKey(e => e.Id);
        change.HasIndex(e => new { e.ProfileId, e.SourceKind, e.SourceId }).IsUnique();
        change.HasOne(e => e.Profile)
            .WithMany()
            .HasForeignKey(e => e.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
