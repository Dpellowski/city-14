namespace Content.Shared._Misfits.CivicPoints;

/// <summary>
/// The public civic title derived from a character's private point balance.
/// </summary>
public enum CivicPriority : byte
{
    Priority1 = 1,
    Priority2 = 2,
    Priority3 = 3,
    Priority4 = 4,
    Priority5 = 5,
    Priority6 = 6,
    Priority7 = 7,
}

public static class CivicPointsRules
{
    public const long StartingPoints = 50;

    public static CivicPriority GetPriority(long points)
    {
        return points switch
        {
            >= 750 => CivicPriority.Priority1,
            >= 500 => CivicPriority.Priority2,
            >= 350 => CivicPriority.Priority3,
            >= 200 => CivicPriority.Priority4,
            >= 100 => CivicPriority.Priority5,
            >= 75 => CivicPriority.Priority6,
            _ => CivicPriority.Priority7,
        };
    }

    public static string TitleLocId(this CivicPriority priority)
    {
        return priority switch
        {
            CivicPriority.Priority1 => "civic-priority-1-title",
            CivicPriority.Priority2 => "civic-priority-2-title",
            CivicPriority.Priority3 => "civic-priority-3-title",
            CivicPriority.Priority4 => "civic-priority-4-title",
            CivicPriority.Priority5 => "civic-priority-5-title",
            CivicPriority.Priority6 => "civic-priority-6-title",
            CivicPriority.Priority7 => "civic-priority-7-title",
            _ => throw new ArgumentOutOfRangeException(nameof(priority), priority, null),
        };
    }
}
