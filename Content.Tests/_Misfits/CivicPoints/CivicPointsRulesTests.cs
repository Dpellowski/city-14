using System;
using System.Linq;
using Content.Shared._Misfits.CivicPoints;
using NUnit.Framework;

namespace Content.Tests._Misfits.CivicPoints;

[TestFixture]
public sealed class CivicPointsRulesTests
{
    [TestCase(long.MinValue, CivicPriority.Priority7)]
    [TestCase(-1, CivicPriority.Priority7)]
    [TestCase(50, CivicPriority.Priority7)]
    [TestCase(74, CivicPriority.Priority7)]
    [TestCase(75, CivicPriority.Priority6)]
    [TestCase(99, CivicPriority.Priority6)]
    [TestCase(100, CivicPriority.Priority5)]
    [TestCase(199, CivicPriority.Priority5)]
    [TestCase(200, CivicPriority.Priority4)]
    [TestCase(349, CivicPriority.Priority4)]
    [TestCase(350, CivicPriority.Priority3)]
    [TestCase(499, CivicPriority.Priority3)]
    [TestCase(500, CivicPriority.Priority2)]
    [TestCase(749, CivicPriority.Priority2)]
    [TestCase(750, CivicPriority.Priority1)]
    [TestCase(long.MaxValue, CivicPriority.Priority1)]
    public void PriorityUsesInclusiveMinimumThresholds(long points, CivicPriority expected)
    {
        Assert.That(CivicPointsRules.GetPriority(points), Is.EqualTo(expected));
    }

    [Test]
    public void CharactersStartAtFiftyAsPrioritySeven()
    {
        Assert.Multiple(() =>
        {
            Assert.That(CivicPointsRules.StartingPoints, Is.EqualTo(50));
            Assert.That(CivicPointsRules.GetPriority(CivicPointsRules.StartingPoints),
                Is.EqualTo(CivicPriority.Priority7));
        });
    }

    [Test]
    public void EveryPriorityHasAStableTitleLocalizationId()
    {
        var priorities = Enum.GetValues<CivicPriority>();
        var ids = priorities.Select(priority => priority.TitleLocId()).ToArray();

        Assert.That(ids, Is.Unique);
        Assert.That(ids, Has.All.StartsWith("civic-priority-"));
    }
}
