using System;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server._Misfits.Characters;
using Content.Server.Database;
using Content.Shared._Misfits.Experience;
using Content.Shared.Preferences;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Robust.Shared.Configuration;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Serialization.Manager;
using Robust.UnitTesting;

namespace Content.IntegrationTests._Misfits.Experience;

[TestFixture]
public sealed class CharacterExperienceDatabaseTests : GameTest
{
    [Test]
    public async Task ExperienceIsIsolatedByPermanentCharacterId()
    {
        var db = GetDb(Pair.Server);
        var accountId = new NetUserId(Guid.NewGuid());

        await db.InitPrefsAsync(accountId, Profile("Morgan One"));
        await db.SaveCharacterSlotAsync(accountId, Profile("Morgan Two"), 1);

        var characters = await db.GetCharactersAsync(accountId);
        var first = characters.Single(character => character.Slot == 0);
        var second = characters.Single(character => character.Slot == 1);

        Assert.That(first.Id, Is.Not.EqualTo(second.Id));

        await db.SetCharacterExperienceAsync(first.Id, ExperienceGroup.Engineering, 450);
        await db.SetCharacterExperienceAsync(second.Id, ExperienceGroup.Medicine, 120);

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(await db.GetCharacterExperienceAsync(first.Id, ExperienceGroup.Engineering), Is.EqualTo(450));
            Assert.That(await db.GetCharacterExperienceAsync(first.Id, ExperienceGroup.Medicine), Is.Null);
            Assert.That(await db.GetCharacterExperienceAsync(second.Id, ExperienceGroup.Engineering), Is.Null);
            Assert.That(await db.GetCharacterExperienceAsync(second.Id, ExperienceGroup.Medicine), Is.EqualTo(120));
        });
    }

    [Test]
    public async Task RenameKeepsIdWhileDeletedSlotGetsFreshId()
    {
        var db = GetDb(Pair.Server);
        var accountId = new NetUserId(Guid.NewGuid());

        await db.InitPrefsAsync(accountId, Profile("Original Name"));
        var original = (await db.GetCharactersAsync(accountId)).Single();
        await db.SetCharacterExperienceAsync(original.Id, ExperienceGroup.Crafting, 225);

        await db.SaveCharacterSlotAsync(accountId, Profile("Renamed Character"), 0);
        var renamed = (await db.GetCharactersAsync(accountId)).Single();

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(renamed.Id, Is.EqualTo(original.Id));
            Assert.That(renamed.Name, Is.EqualTo("Renamed Character"));
            Assert.That(await db.GetCharacterExperienceAsync(renamed.Id, ExperienceGroup.Crafting), Is.EqualTo(225));
        });

        await db.SaveCharacterSlotAsync(accountId, null, 0);
        await db.SaveCharacterSlotAsync(accountId, Profile("Replacement Character"), 0);
        var replacement = (await db.GetCharactersAsync(accountId)).Single();

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(replacement.Id, Is.Not.EqualTo(original.Id));
            Assert.That(await db.GetCharacterExperienceAsync(replacement.Id, ExperienceGroup.Crafting), Is.Null);
        });
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
