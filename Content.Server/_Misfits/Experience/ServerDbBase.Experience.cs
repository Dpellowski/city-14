using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server._Misfits.Characters;
using Content.Server.Database._Misfits.Experience;
using Content.Shared._Misfits.Experience;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database;

public abstract partial class ServerDbBase
{
    public async Task<IReadOnlyDictionary<ExperienceGroup, long>> GetCharacterExperienceAsync(
        CharacterId characterId,
        CancellationToken cancel = default)
    {
        await using var db = await GetDb(cancel);
        var rows = await db.DbContext.Set<CharacterExperienceModel>()
            .Where(experience => experience.ProfileId == characterId.Value)
            .Select(experience => new { experience.ExperienceGroup, experience.TotalExperience })
            .ToListAsync(cancel);

        var result = new Dictionary<ExperienceGroup, long>();
        foreach (var row in rows)
        {
            if (ExperienceGroupExtensions.TryParse(row.ExperienceGroup, out var group))
                result[group] = row.TotalExperience;
        }

        return result;
    }

    public async Task<long?> GetCharacterExperienceAsync(
        CharacterId characterId,
        ExperienceGroup group,
        CancellationToken cancel = default)
    {
        await using var db = await GetDb(cancel);
        return await db.DbContext.Set<CharacterExperienceModel>()
            .Where(experience => experience.ProfileId == characterId.Value &&
                                 experience.ExperienceGroup == group.Id())
            .Select(experience => (long?) experience.TotalExperience)
            .SingleOrDefaultAsync(cancel);
    }

    public async Task<bool> SetCharacterExperienceAsync(
        CharacterId characterId,
        ExperienceGroup group,
        long totalExperience,
        CancellationToken cancel = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(totalExperience);

        await using var db = await GetDb(cancel);
        if (!await db.DbContext.Profile.AnyAsync(profile => profile.Id == characterId.Value, cancel))
            return false;

        var groupId = group.Id();
        var existing = await db.DbContext.Set<CharacterExperienceModel>()
            .SingleOrDefaultAsync(experience => experience.ProfileId == characterId.Value &&
                                                experience.ExperienceGroup == groupId,
                cancel);

        if (existing == null)
        {
            db.DbContext.Set<CharacterExperienceModel>().Add(new CharacterExperienceModel
            {
                ProfileId = characterId.Value,
                ExperienceGroup = groupId,
                TotalExperience = totalExperience,
            });
        }
        else
        {
            existing.TotalExperience = totalExperience;
        }

        await db.DbContext.SaveChangesAsync(cancel);
        return true;
    }
}
