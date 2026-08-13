using System;
using Content.Server._Misfits.CivicPoints;
using NUnit.Framework;

namespace Content.Tests._Misfits.CivicPoints;

[TestFixture]
public sealed class CivicPointsSourceTests
{
    [Test]
    public void TaskSourceUsesStableIdempotencyKey()
    {
        var source = CivicPointsSource.Task("task-123", "completed", "Test completion");

        Assert.Multiple(() =>
        {
            Assert.That(source.Kind, Is.EqualTo(CivicPointsSource.TaskKind));
            Assert.That(source.Id, Is.EqualTo("task-123:completed"));
            Assert.That(source.Reason, Is.EqualTo("Test completion"));
        });
    }

    [TestCase("", "completed")]
    [TestCase(" ", "completed")]
    [TestCase("task-123", "")]
    [TestCase("task-123", " ")]
    public void TaskSourceRejectsIncompleteIdempotencyKey(string taskInstanceId, string transition)
    {
        Assert.Throws<ArgumentException>(() =>
            CivicPointsSource.Task(taskInstanceId, transition, "Test completion"));
    }
}
