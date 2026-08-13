namespace Content.Shared._Misfits.Experience;

/// <summary>
/// Defines the uncapped character experience curve.
/// Characters begin at level one and every next level costs 25 XP more than the previous one.
/// </summary>
public static class ExperienceMath
{
    public const long StartingLevel = 1;
    public const long BaseLevelCost = 100;
    public const long LevelCostIncrease = 25;

    // The highest BIGINT-representable threshold is below this guard. Checking first
    // also keeps the decimal curve calculation in range for arbitrary API callers.
    private const long StorageRangeLevelGuard = 1_000_000_000;

    public static long GetLevel(long totalExperience)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(totalExperience);

        long lower = StartingLevel;
        long upper = StartingLevel + 1;

        while (ExperienceForLevelDecimal(upper) <= totalExperience)
        {
            lower = upper;
            upper = checked(upper * 2);
        }

        while (lower + 1 < upper)
        {
            var middle = lower + (upper - lower) / 2;
            if (ExperienceForLevelDecimal(middle) <= totalExperience)
                lower = middle;
            else
                upper = middle;
        }

        return lower;
    }

    public static long ExperienceForLevel(long level)
    {
        if (level is < StartingLevel or > StorageRangeLevelGuard)
            throw new ArgumentOutOfRangeException(nameof(level), level,
                "The requested level is outside the experience storage range.");

        var result = ExperienceForLevelDecimal(level);
        if (result > long.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(level), level,
                "The requested level is outside the experience storage range.");

        return decimal.ToInt64(result);
    }

    public static long ExperienceToNextLevel(long level)
    {
        if (level < StartingLevel)
            throw new ArgumentOutOfRangeException(nameof(level));

        return checked(BaseLevelCost + LevelCostIncrease * (level - StartingLevel));
    }

    public static ExperienceProgress GetProgress(long totalExperience)
    {
        var level = GetLevel(totalExperience);
        var levelStart = ExperienceForLevel(level);
        var required = ExperienceToNextLevel(level);
        return new ExperienceProgress(level, totalExperience, totalExperience - levelStart, required);
    }

    private static decimal ExperienceForLevelDecimal(long level)
    {
        var completedLevels = (decimal) level - StartingLevel;
        return BaseLevelCost * completedLevels +
               LevelCostIncrease * completedLevels * (completedLevels - 1) / 2;
    }
}

public readonly record struct ExperienceProgress(
    long Level,
    long TotalExperience,
    long CurrentLevelExperience,
    long ExperienceToNextLevel)
{
    public float Fraction => ExperienceToNextLevel == 0
        ? 0f
        : (float) CurrentLevelExperience / ExperienceToNextLevel;
}
