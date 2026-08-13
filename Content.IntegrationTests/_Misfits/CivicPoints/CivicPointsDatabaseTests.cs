using System;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server._Misfits.CivicPoints;
using Content.Server.Database;
using Content.Shared._Misfits.CivicPoints;
using Content.Shared.Preferences;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Robust.Shared.Configuration;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Serialization.Manager;
using Robust.UnitTesting;

namespace Content.IntegrationTests._Misfits.CivicPoints;

[TestFixture]
public sealed class CivicPointsDatabaseTests : GameTest
{
    [Test]
    public async Task NewCharactersStartAtFiftyAndRemainIsolated()
    {
        var db = GetDb(Pair.Server);
        var accountId = new NetUserId(Guid.NewGuid());

        await db.InitPrefsAsync(accountId, Profile("Morgan One"));
        await db.SaveCharacterSlotAsync(accountId, Profile("Morgan Two"), 1);

        var characters = await db.GetCharactersAsync(accountId);
        var first = characters.Single(character => character.Slot == 0);
        var second = characters.Single(character => character.Slot == 1);

        Assert.Multiple(() =>
        {
            Assert.That(first.Id, Is.Not.EqualTo(second.Id));
            Assert.That(CivicPointsRules.StartingPoints, Is.EqualTo(50));
        });
        await Assert.MultipleAsync(async () =>
        {
            Assert.That(await db.GetCharacterCivicPointsAsync(first.Id), Is.EqualTo(50));
            Assert.That(await db.GetCharacterCivicPointsAsync(second.Id), Is.EqualTo(50));
        });
    }

    [Test]
    public async Task ChangesCanGoNegativeAndHaveNoTierMaximum()
    {
        var db = GetDb(Pair.Server);
        var accountId = new NetUserId(Guid.NewGuid());
        await db.InitPrefsAsync(accountId, Profile("Morgan"));
        var character = (await db.GetCharactersAsync(accountId)).Single();

        var removed = await db.MutateCharacterCivicPointsAsync(
            character.Id,
            CivicPointsMutation.Delta,
            -100,
            Source("negative"));
        Assert.Multiple(() =>
        {
            Assert.That(removed, Is.Not.Null);
            Assert.That(removed!.PreviousPoints, Is.EqualTo(50));
            Assert.That(removed.NewPoints, Is.EqualTo(-50));
            Assert.That(removed.Applied, Is.True);
        });

        await db.MutateCharacterCivicPointsAsync(
            character.Id,
            CivicPointsMutation.Set,
            CivicPointsRules.StartingPoints,
            Source("reset"));

        var maximum = await db.MutateCharacterCivicPointsAsync(
            character.Id,
            CivicPointsMutation.Set,
            long.MaxValue,
            Source("maximum"));
        Assert.Multiple(() =>
        {
            Assert.That(maximum, Is.Not.Null);
            Assert.That(maximum!.NewPoints, Is.EqualTo(long.MaxValue));
            Assert.That(CivicPointsRules.GetPriority(maximum.NewPoints), Is.EqualTo(CivicPriority.Priority1));
        });
        Assert.That(await db.GetCharacterCivicPointsAsync(character.Id), Is.EqualTo(long.MaxValue));
    }

    [Test]
    public async Task TaskSourceIsIdempotentAndCannotBeReusedForAnotherMutation()
    {
        var db = GetDb(Pair.Server);
        var accountId = new NetUserId(Guid.NewGuid());
        await db.InitPrefsAsync(accountId, Profile("Morgan"));
        var character = (await db.GetCharactersAsync(accountId)).Single();
        var source = Source("task-123-completed");

        var first = await db.MutateCharacterCivicPointsAsync(
            character.Id,
            CivicPointsMutation.Delta,
            25,
            source);
        var duplicate = await db.MutateCharacterCivicPointsAsync(
            character.Id,
            CivicPointsMutation.Delta,
            25,
            source);

        Assert.Multiple(() =>
        {
            Assert.That(first?.Applied, Is.True);
            Assert.That(first?.NewPoints, Is.EqualTo(75));
            Assert.That(duplicate?.Applied, Is.False);
            Assert.That(duplicate?.NewPoints, Is.EqualTo(75));
        });
        Assert.That(await db.GetCharacterCivicPointsAsync(character.Id), Is.EqualTo(75));

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await db.MutateCharacterCivicPointsAsync(
                character.Id,
                CivicPointsMutation.Delta,
                30,
                source));
    }

    [Test]
    public async Task RenamePreservesPointsWhileRecreatedSlotStartsFresh()
    {
        var db = GetDb(Pair.Server);
        var accountId = new NetUserId(Guid.NewGuid());

        await db.InitPrefsAsync(accountId, Profile("Original"));
        var original = (await db.GetCharactersAsync(accountId)).Single();
        await db.MutateCharacterCivicPointsAsync(
            original.Id,
            CivicPointsMutation.Set,
            350,
            Source("set-priority-three"));

        await db.SaveCharacterSlotAsync(accountId, Profile("Renamed"), 0);
        var renamed = (await db.GetCharactersAsync(accountId)).Single();
        Assert.That(renamed.Id, Is.EqualTo(original.Id));
        Assert.That(await db.GetCharacterCivicPointsAsync(renamed.Id), Is.EqualTo(350));

        await db.SaveCharacterSlotAsync(accountId, null, 0);
        Assert.That(await db.GetCharacterCivicPointsAsync(original.Id), Is.Null);

        await db.SaveCharacterSlotAsync(accountId, Profile("Replacement"), 0);
        var replacement = (await db.GetCharactersAsync(accountId)).Single();
        Assert.Multiple(() =>
        {
            Assert.That(replacement.Id, Is.Not.EqualTo(original.Id));
            Assert.That(replacement.Name, Is.EqualTo("Replacement"));
        });
        Assert.That(await db.GetCharacterCivicPointsAsync(replacement.Id), Is.EqualTo(50));
    }

    private static CivicPointsSource Source(string id)
    {
        return new CivicPointsSource(CivicPointsSource.TaskKind, id, "Integration test");
    }

    private static HumanoidCharacterProfile Profile(string name)
    {
        return new HumanoidCharacterProfile { Name = name };
    }

    private static ServerDbSqlite GetDb(RobustIntegrationTest.ServerIntegrationInstance server)
    {
        var cfg = server.ResolveDependency<IConfigurationManager>();
        var serialization = server.ResolveDependency<ISerializationManager>();
        var opsLog = server.ResolveDependency<ILogManager>().GetSawmill("db.ops");
        var builder = new DbContextOptionsBuilder<SqliteServerDbContext>();
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        builder.UseSqlite(connection);
        return new ServerDbSqlite(() => builder.Options, true, cfg, true, opsLog, serialization);
    }
}
