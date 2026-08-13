using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Content.Server._Misfits.Characters;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server._Misfits.Experience;

public sealed record CharacterExperienceChange(
    CharacterSummary Character,
    Content.Shared._Misfits.Experience.ExperienceGroup Group,
    long PreviousExperience,
    long NewExperience,
    long PreviousLevel,
    long NewLevel);

public interface ICharacterExperienceDbManager
{
    Task<IReadOnlyDictionary<Content.Shared._Misfits.Experience.ExperienceGroup, long>> GetCharacterExperienceAsync(
        CharacterId characterId,
        CancellationToken cancel = default);

    Task<long?> GetCharacterExperienceAsync(
        CharacterId characterId,
        Content.Shared._Misfits.Experience.ExperienceGroup group,
        CancellationToken cancel = default);

    Task<bool> SetCharacterExperienceAsync(
        CharacterId characterId,
        Content.Shared._Misfits.Experience.ExperienceGroup group,
        long totalExperience,
        CancellationToken cancel = default);
}

public interface ICharacterExperienceManager
{
    void Initialize();

    Task<IReadOnlyList<CharacterSummary>> GetCharactersAsync(
        NetUserId accountId,
        CancellationToken cancel = default);

    Task<IReadOnlyDictionary<Content.Shared._Misfits.Experience.ExperienceGroup, long>?> GetExperienceAsync(
        CharacterId characterId,
        CancellationToken cancel = default);

    Task<CharacterExperienceChange?> SetExperienceAsync(
        CharacterId characterId,
        Content.Shared._Misfits.Experience.ExperienceGroup group,
        long totalExperience,
        CancellationToken cancel = default);

    Task<CharacterExperienceChange?> AddExperienceAsync(
        CharacterId characterId,
        Content.Shared._Misfits.Experience.ExperienceGroup group,
        long amount,
        CancellationToken cancel = default);

    Task<CharacterExperienceChange?> RemoveExperienceAsync(
        CharacterId characterId,
        Content.Shared._Misfits.Experience.ExperienceGroup group,
        long amount,
        CancellationToken cancel = default);

    Task<CharacterExperienceChange?> SetExperienceAsync(
        ICommonSession session,
        Content.Shared._Misfits.Experience.ExperienceGroup group,
        long totalExperience,
        CancellationToken cancel = default);

    Task<CharacterExperienceChange?> AddExperienceAsync(
        ICommonSession session,
        Content.Shared._Misfits.Experience.ExperienceGroup group,
        long amount,
        CancellationToken cancel = default);

    Task<CharacterExperienceChange?> RemoveExperienceAsync(
        ICommonSession session,
        Content.Shared._Misfits.Experience.ExperienceGroup group,
        long amount,
        CancellationToken cancel = default);

    Task<CharacterExperienceChange?> AddExperienceAsync(
        EntityUid player,
        Content.Shared._Misfits.Experience.ExperienceGroup group,
        long amount,
        CancellationToken cancel = default);

    Task<CharacterExperienceChange?> SetExperienceAsync(
        EntityUid player,
        Content.Shared._Misfits.Experience.ExperienceGroup group,
        long totalExperience,
        CancellationToken cancel = default);

    Task<CharacterExperienceChange?> RemoveExperienceAsync(
        EntityUid player,
        Content.Shared._Misfits.Experience.ExperienceGroup group,
        long amount,
        CancellationToken cancel = default);
}
