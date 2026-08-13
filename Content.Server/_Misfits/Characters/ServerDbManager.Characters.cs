using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Content.Server._Misfits.Characters;
using Robust.Shared.Network;

namespace Content.Server.Database;

public sealed partial class ServerDbManager
{
    public Task<IReadOnlyList<CharacterSummary>> GetCharactersAsync(
        NetUserId accountId,
        CancellationToken cancel = default)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetCharactersAsync(accountId, cancel));
    }

    public Task<CharacterSummary?> GetCharacterAsync(
        CharacterId characterId,
        CancellationToken cancel = default)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetCharacterAsync(characterId, cancel));
    }

    public Task<CharacterSummary?> GetCharacterAsync(
        NetUserId accountId,
        int slot,
        CancellationToken cancel = default)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetCharacterAsync(accountId, slot, cancel));
    }

    public Task<CharacterSummary?> GetSelectedCharacterAsync(
        NetUserId accountId,
        CancellationToken cancel = default)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetSelectedCharacterAsync(accountId, cancel));
    }
}
