namespace Content.Shared._Misfits.Experience;

/// <summary>
/// Persistent experience tracks available to a character.
/// Explicit values keep network serialization stable.
/// </summary>
public enum ExperienceGroup : byte
{
    Industrial = 0,
    Engineering = 1,
    InfestationControl = 2,
    CivilPatrol = 3,
    SecurityAdministration = 4,
    Resistance = 5,
    Cooking = 6,
    Medicine = 7,
    Scavenging = 8,
    Crafting = 9,
}

public enum ExperienceCategory : byte
{
    Faction,
    Character,
}

public static class ExperienceGroupExtensions
{
    public static readonly ExperienceGroup[] All =
    [
        ExperienceGroup.Industrial,
        ExperienceGroup.Engineering,
        ExperienceGroup.InfestationControl,
        ExperienceGroup.CivilPatrol,
        ExperienceGroup.SecurityAdministration,
        ExperienceGroup.Resistance,
        ExperienceGroup.Cooking,
        ExperienceGroup.Medicine,
        ExperienceGroup.Scavenging,
        ExperienceGroup.Crafting,
    ];

    public static readonly ExperienceGroup[] Faction =
    [
        ExperienceGroup.Industrial,
        ExperienceGroup.Engineering,
        ExperienceGroup.InfestationControl,
        ExperienceGroup.CivilPatrol,
        ExperienceGroup.SecurityAdministration,
        ExperienceGroup.Resistance,
    ];

    public static readonly ExperienceGroup[] Character =
    [
        ExperienceGroup.Cooking,
        ExperienceGroup.Medicine,
        ExperienceGroup.Scavenging,
        ExperienceGroup.Crafting,
    ];

    public static ExperienceCategory Category(this ExperienceGroup group)
    {
        return group <= ExperienceGroup.Resistance
            ? ExperienceCategory.Faction
            : ExperienceCategory.Character;
    }

    public static string Id(this ExperienceGroup group)
    {
        return group switch
        {
            ExperienceGroup.Industrial => "industrial",
            ExperienceGroup.Engineering => "engineering",
            ExperienceGroup.InfestationControl => "infestation-control",
            ExperienceGroup.CivilPatrol => "civil-patrol",
            ExperienceGroup.SecurityAdministration => "security-administration",
            ExperienceGroup.Resistance => "resistance",
            ExperienceGroup.Cooking => "cooking",
            ExperienceGroup.Medicine => "medicine",
            ExperienceGroup.Scavenging => "scavenging",
            ExperienceGroup.Crafting => "crafting",
            _ => throw new ArgumentOutOfRangeException(nameof(group), group, null),
        };
    }

    public static bool TryParse(string value, out ExperienceGroup group)
    {
        var normalized = value.Trim().Replace('_', '-').ToLowerInvariant();
        foreach (var candidate in All)
        {
            if (candidate.Id() == normalized ||
                candidate.ToString().Equals(value, StringComparison.OrdinalIgnoreCase))
            {
                group = candidate;
                return true;
            }
        }

        group = default;
        return false;
    }

}
