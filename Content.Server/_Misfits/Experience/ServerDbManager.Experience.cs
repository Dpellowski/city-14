using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Content.Server._Misfits.Characters;
using Content.Shared._Misfits.Experience;

namespace Content.Server.Database;

public sealed partial class ServerDbManager
{
    public Task<IReadOnlyDictionary<ExperienceGroup, long>> GetCharacterExperienceAsync(
        CharacterId characterId,
        CancellationToken cancel = default)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetCharacterExperienceAsync(characterId, cancel));
    }

    public Task<long?> GetCharacterExperienceAsync(
        CharacterId characterId,
        ExperienceGroup group,
        CancellationToken cancel = default)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetCharacterExperienceAsync(characterId, group, cancel));
    }

    public Task<bool> SetCharacterExperienceAsync(
        CharacterId characterId,
        ExperienceGroup group,
        long totalExperience,
        CancellationToken cancel = default)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.SetCharacterExperienceAsync(characterId, group, totalExperience, cancel));
    }
}
