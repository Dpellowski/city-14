using System.Threading;
using System.Threading.Tasks;
using Content.Server._Misfits.Characters;
using Content.Server._Misfits.CivicPoints;

namespace Content.Server.Database;

public sealed partial class ServerDbManager
{
    public Task<long?> GetCharacterCivicPointsAsync(
        CharacterId characterId,
        CancellationToken cancel = default)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetCharacterCivicPointsAsync(characterId, cancel));
    }

    public Task<CivicPointsDbChange?> MutateCharacterCivicPointsAsync(
        CharacterId characterId,
        CivicPointsMutation mutation,
        long value,
        CivicPointsSource source,
        CancellationToken cancel = default)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.MutateCharacterCivicPointsAsync(
            characterId,
            mutation,
            value,
            source,
            cancel));
    }
}
