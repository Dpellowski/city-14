using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server._Misfits.Characters;
using Microsoft.EntityFrameworkCore;
using Robust.Shared.Network;

namespace Content.Server.Database;

public abstract partial class ServerDbBase
{
    public async Task<IReadOnlyList<CharacterSummary>> GetCharactersAsync(
        NetUserId accountId,
        CancellationToken cancel = default)
    {
        await using var db = await GetDb(cancel);
        var profiles = await db.DbContext.Profile
            .Where(profile => profile.Preference.UserId == accountId.UserId)
            .Select(profile => new
            {
                profile.Id,
                profile.Slot,
                profile.CharacterName,
                profile.Preference.SelectedCharacterSlot,
            })
            .OrderBy(profile => profile.Slot)
            .ToListAsync(cancel);

        return profiles
            .Select(profile => new CharacterSummary(
                new CharacterId(profile.Id),
                accountId,
                profile.Slot,
                profile.CharacterName,
                profile.Slot == profile.SelectedCharacterSlot))
            .ToList();
    }

    public async Task<CharacterSummary?> GetCharacterAsync(
        CharacterId characterId,
        CancellationToken cancel = default)
    {
        await using var db = await GetDb(cancel);
        var profile = await db.DbContext.Profile
            .Where(profile => profile.Id == characterId.Value)
            .Select(profile => new
            {
                profile.Id,
                profile.Slot,
                profile.CharacterName,
                profile.Preference.UserId,
                profile.Preference.SelectedCharacterSlot,
            })
            .SingleOrDefaultAsync(cancel);

        if (profile == null)
            return null;

        return new CharacterSummary(
            new CharacterId(profile.Id),
            new NetUserId(profile.UserId),
            profile.Slot,
            profile.CharacterName,
            profile.Slot == profile.SelectedCharacterSlot);
    }

    public async Task<CharacterSummary?> GetCharacterAsync(
        NetUserId accountId,
        int slot,
        CancellationToken cancel = default)
    {
        await using var db = await GetDb(cancel);
        var profile = await db.DbContext.Profile
            .Where(profile => profile.Preference.UserId == accountId.UserId && profile.Slot == slot)
            .Select(profile => new
            {
                profile.Id,
                profile.Slot,
                profile.CharacterName,
                profile.Preference.SelectedCharacterSlot,
            })
            .SingleOrDefaultAsync(cancel);

        if (profile == null)
            return null;

        return new CharacterSummary(
            new CharacterId(profile.Id),
            accountId,
            profile.Slot,
            profile.CharacterName,
            profile.Slot == profile.SelectedCharacterSlot);
    }

    public async Task<CharacterSummary?> GetSelectedCharacterAsync(
        NetUserId accountId,
        CancellationToken cancel = default)
    {
        await using var db = await GetDb(cancel);
        var profile = await db.DbContext.Profile
            .Where(profile => profile.Preference.UserId == accountId.UserId &&
                              profile.Slot == profile.Preference.SelectedCharacterSlot)
            .Select(profile => new
            {
                profile.Id,
                profile.Slot,
                profile.CharacterName,
            })
            .SingleOrDefaultAsync(cancel);

        if (profile == null)
            return null;

        return new CharacterSummary(
            new CharacterId(profile.Id),
            accountId,
            profile.Slot,
            profile.CharacterName,
            true);
    }
}
