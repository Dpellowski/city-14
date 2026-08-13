using System;
using System.Linq;
using Content.Shared._Misfits.Experience;
using NUnit.Framework;

namespace Content.Tests._Misfits.Experience;

[TestFixture]
public sealed class ExperienceMathTests
{
    [TestCase(1, 0)]
    [TestCase(2, 100)]
    [TestCase(3, 225)]
    [TestCase(4, 375)]
    [TestCase(10, 1800)]
    public void LevelThresholdsIncreaseFairly(long level, long expectedExperience)
    {
        Assert.That(ExperienceMath.ExperienceForLevel(level), Is.EqualTo(expectedExperience));
    }

    [TestCase(0, 1)]
    [TestCase(99, 1)]
    [TestCase(100, 2)]
    [TestCase(224, 2)]
    [TestCase(225, 3)]
    [TestCase(1800, 10)]
    public void TotalExperienceDerivesLevel(long totalExperience, long expectedLevel)
    {
        Assert.That(ExperienceMath.GetLevel(totalExperience), Is.EqualTo(expectedLevel));
    }

    [Test]
    public void ProgressIsRelativeToCurrentLevel()
    {
        var progress = ExperienceMath.GetProgress(150);

        Assert.Multiple(() =>
        {
            Assert.That(progress.Level, Is.EqualTo(2));
            Assert.That(progress.CurrentLevelExperience, Is.EqualTo(50));
            Assert.That(progress.ExperienceToNextLevel, Is.EqualTo(125));
            Assert.That(progress.Fraction, Is.EqualTo(0.4f).Within(0.001f));
        });
    }

    [Test]
    public void CurveSupportsEntireStorageRangeWithoutConfiguredLevelCap()
    {
        var progress = ExperienceMath.GetProgress(long.MaxValue);

        Assert.That(progress.Level, Is.GreaterThan(1));
        Assert.That(progress.TotalExperience, Is.EqualTo(long.MaxValue));
        Assert.That(progress.Fraction, Is.InRange(0f, 1f));
    }

    [Test]
    public void UnstorableLevelUsesSandboxApprovedRangeFailure()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ExperienceMath.ExperienceForLevel(1_000_000_000));
    }

    [Test]
    public void GroupsHaveStableUniqueIdsAndExpectedCategories()
    {
        var ids = ExperienceGroupExtensions.All.Select(group => group.Id()).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(ids, Is.Unique);
            Assert.That(ExperienceGroupExtensions.Faction, Has.Length.EqualTo(6));
            Assert.That(ExperienceGroupExtensions.Character, Has.Length.EqualTo(4));
            Assert.That(ExperienceGroup.Engineering.Category(), Is.EqualTo(ExperienceCategory.Faction));
            Assert.That(ExperienceGroup.Medicine.Category(), Is.EqualTo(ExperienceCategory.Character));
        });
    }

    [TestCase("infestation-control", ExperienceGroup.InfestationControl)]
    [TestCase("security_administration", ExperienceGroup.SecurityAdministration)]
    [TestCase("Crafting", ExperienceGroup.Crafting)]
    public void GroupParserAcceptsCommandFriendlyNames(string input, ExperienceGroup expected)
    {
        Assert.That(ExperienceGroupExtensions.TryParse(input, out var actual), Is.True);
        Assert.That(actual, Is.EqualTo(expected));
    }
}
