using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server._Misfits.Characters;

/// <summary>
/// Stable database identity for a saved character profile.
/// </summary>
public readonly record struct CharacterId(int Value)
{
    public override string ToString()
    {
        return Value.ToString();
    }
}

public sealed record CharacterSummary(
    CharacterId Id,
    NetUserId AccountId,
    int Slot,
    string Name,
    bool Selected);

public interface IPermanentCharacterDbManager
{
    Task<IReadOnlyList<CharacterSummary>> GetCharactersAsync(
        NetUserId accountId,
        CancellationToken cancel = default);

    Task<CharacterSummary?> GetCharacterAsync(
        CharacterId characterId,
        CancellationToken cancel = default);

    Task<CharacterSummary?> GetCharacterAsync(
        NetUserId accountId,
        int slot,
        CancellationToken cancel = default);

    Task<CharacterSummary?> GetSelectedCharacterAsync(
        NetUserId accountId,
        CancellationToken cancel = default);
}

public interface IPermanentCharacterManager
{
    Task BindActiveCharacterAsync(
        ICommonSession session,
        CancellationToken cancel = default);

    void ClearActiveCharacters();

    Task<IReadOnlyList<CharacterSummary>> GetCharactersAsync(
        NetUserId accountId,
        CancellationToken cancel = default);

    Task<CharacterSummary?> GetCharacterAsync(
        CharacterId characterId,
        CancellationToken cancel = default);

    Task<CharacterSummary?> GetCurrentCharacterAsync(
        NetUserId accountId,
        CancellationToken cancel = default);
}
