using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Database;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server._Misfits.Characters;

public sealed partial class PermanentCharacterManager : IPermanentCharacterManager
{
    [Dependency] private IServerDbManager _db = default!;

    private readonly ConcurrentDictionary<NetUserId, CharacterId> _activeCharacters = new();

    public async Task BindActiveCharacterAsync(
        ICommonSession session,
        CancellationToken cancel = default)
    {
        var character = await _db.GetSelectedCharacterAsync(session.UserId, cancel);
        if (character == null)
        {
            _activeCharacters.TryRemove(session.UserId, out _);
            return;
        }

        _activeCharacters[session.UserId] = character.Id;
    }

    public void ClearActiveCharacters()
    {
        _activeCharacters.Clear();
    }

    public Task<IReadOnlyList<CharacterSummary>> GetCharactersAsync(
        NetUserId accountId,
        CancellationToken cancel = default)
    {
        return _db.GetCharactersAsync(accountId, cancel);
    }

    public Task<CharacterSummary?> GetCharacterAsync(
        CharacterId characterId,
        CancellationToken cancel = default)
    {
        return _db.GetCharacterAsync(characterId, cancel);
    }

    public async Task<CharacterSummary?> GetCurrentCharacterAsync(
        NetUserId accountId,
        CancellationToken cancel = default)
    {
        if (_activeCharacters.TryGetValue(accountId, out var activeId))
        {
            var active = await _db.GetCharacterAsync(activeId, cancel);
            if (active?.AccountId == accountId)
                return active;

            _activeCharacters.TryRemove(accountId, out _);
        }

        return await _db.GetSelectedCharacterAsync(accountId, cancel);
    }
}
