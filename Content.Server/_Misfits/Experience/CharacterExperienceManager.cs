using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Content.Server._Misfits.Characters;
using Content.Server.Database;
using Content.Shared._Misfits.Experience;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server._Misfits.Experience;

public sealed partial class CharacterExperienceManager : ICharacterExperienceManager
{
    [Dependency] private IServerDbManager _db = default!;
    [Dependency] private ILogManager _logManager = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private IPlayerManager _players = default!;

    private readonly ConcurrentDictionary<(CharacterId Character, ExperienceGroup Group), SemaphoreSlim> _mutationLocks = new();
    [Dependency] private IPermanentCharacterManager _characters = default!;
    private ISawmill _sawmill = default!;

    public void Initialize()
    {
        _sawmill = _logManager.GetSawmill("misfits.experience");
        _net.RegisterNetMessage<MsgCharacterExperienceRequest>(HandleSnapshotRequest);
        _net.RegisterNetMessage<MsgCharacterExperienceUpdate>();
    }

    public Task<IReadOnlyList<CharacterSummary>> GetCharactersAsync(
        NetUserId accountId,
        CancellationToken cancel = default)
    {
        return _characters.GetCharactersAsync(accountId, cancel);
    }

    public async Task<IReadOnlyDictionary<ExperienceGroup, long>?> GetExperienceAsync(
        CharacterId characterId,
        CancellationToken cancel = default)
    {
        if (await _db.GetCharacterAsync(characterId, cancel) == null)
            return null;

        var stored = await _db.GetCharacterExperienceAsync(characterId, cancel);
        var result = new Dictionary<ExperienceGroup, long>(ExperienceGroupExtensions.All.Length);
        foreach (var group in ExperienceGroupExtensions.All)
        {
            result[group] = stored.GetValueOrDefault(group);
        }

        return result;
    }

    public Task<CharacterExperienceChange?> SetExperienceAsync(
        CharacterId characterId,
        ExperienceGroup group,
        long totalExperience,
        CancellationToken cancel = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(totalExperience);
        return MutateExperienceAsync(characterId, group, ExperienceMutation.Set, totalExperience, cancel);
    }

    public Task<CharacterExperienceChange?> AddExperienceAsync(
        CharacterId characterId,
        ExperienceGroup group,
        long amount,
        CancellationToken cancel = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        return MutateExperienceAsync(characterId, group, ExperienceMutation.Add, amount, cancel);
    }

    public Task<CharacterExperienceChange?> RemoveExperienceAsync(
        CharacterId characterId,
        ExperienceGroup group,
        long amount,
        CancellationToken cancel = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        return MutateExperienceAsync(characterId, group, ExperienceMutation.Remove, amount, cancel);
    }

    public async Task<CharacterExperienceChange?> SetExperienceAsync(
        ICommonSession session,
        ExperienceGroup group,
        long totalExperience,
        CancellationToken cancel = default)
    {
        var character = await _characters.GetCurrentCharacterAsync(session.UserId, cancel);
        return character == null
            ? null
            : await SetExperienceAsync(character.Id, group, totalExperience, cancel);
    }

    public async Task<CharacterExperienceChange?> AddExperienceAsync(
        ICommonSession session,
        ExperienceGroup group,
        long amount,
        CancellationToken cancel = default)
    {
        var character = await _characters.GetCurrentCharacterAsync(session.UserId, cancel);
        return character == null
            ? null
            : await AddExperienceAsync(character.Id, group, amount, cancel);
    }

    public async Task<CharacterExperienceChange?> RemoveExperienceAsync(
        ICommonSession session,
        ExperienceGroup group,
        long amount,
        CancellationToken cancel = default)
    {
        var character = await _characters.GetCurrentCharacterAsync(session.UserId, cancel);
        return character == null
            ? null
            : await RemoveExperienceAsync(character.Id, group, amount, cancel);
    }

    public Task<CharacterExperienceChange?> AddExperienceAsync(
        EntityUid player,
        ExperienceGroup group,
        long amount,
        CancellationToken cancel = default)
    {
        if (!_players.TryGetSessionByEntity(player, out var session))
            return Task.FromResult<CharacterExperienceChange?>(null);

        return AddExperienceAsync(session, group, amount, cancel);
    }

    public Task<CharacterExperienceChange?> SetExperienceAsync(
        EntityUid player,
        ExperienceGroup group,
        long totalExperience,
        CancellationToken cancel = default)
    {
        if (!_players.TryGetSessionByEntity(player, out var session))
            return Task.FromResult<CharacterExperienceChange?>(null);

        return SetExperienceAsync(session, group, totalExperience, cancel);
    }

    public Task<CharacterExperienceChange?> RemoveExperienceAsync(
        EntityUid player,
        ExperienceGroup group,
        long amount,
        CancellationToken cancel = default)
    {
        if (!_players.TryGetSessionByEntity(player, out var session))
            return Task.FromResult<CharacterExperienceChange?>(null);

        return RemoveExperienceAsync(session, group, amount, cancel);
    }

    private async Task<CharacterExperienceChange?> MutateExperienceAsync(
        CharacterId characterId,
        ExperienceGroup group,
        ExperienceMutation mutation,
        long value,
        CancellationToken cancel)
    {
        // Validate the enum before touching storage.
        _ = group.Id();

        var gate = _mutationLocks.GetOrAdd((characterId, group), _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancel);

        CharacterExperienceChange? change;
        try
        {
            var character = await _db.GetCharacterAsync(characterId, cancel);
            if (character == null)
                return null;

            var previous = await _db.GetCharacterExperienceAsync(characterId, group, cancel) ?? 0;
            var next = mutation switch
            {
                ExperienceMutation.Set => value,
                ExperienceMutation.Add => checked(previous + value),
                ExperienceMutation.Remove => value >= previous ? 0 : previous - value,
                _ => throw new ArgumentOutOfRangeException(nameof(mutation), mutation, null),
            };

            if (!await _db.SetCharacterExperienceAsync(characterId, group, next, cancel))
                return null;

            change = new CharacterExperienceChange(
                character,
                group,
                previous,
                next,
                ExperienceMath.GetLevel(previous),
                ExperienceMath.GetLevel(next));
        }
        finally
        {
            gate.Release();
        }

        await PushUpdateIfSelectedAsync(change.Character, cancel);
        return change;
    }

    private async void HandleSnapshotRequest(MsgCharacterExperienceRequest message)
    {
        try
        {
            var character = await _characters.GetCurrentCharacterAsync(message.MsgChannel.UserId);
            if (character == null)
            {
                _net.ServerSendMessage(new MsgCharacterExperienceUpdate(), message.MsgChannel);
                return;
            }

            await SendSnapshotAsync(character, message.MsgChannel);
        }
        catch (Exception exception)
        {
            _sawmill.Error("Unable to load character experience for {User}: {Exception}",
                message.MsgChannel.UserId,
                exception);
        }
    }

    private async Task PushUpdateIfSelectedAsync(CharacterSummary character, CancellationToken cancel)
    {
        if (!_players.TryGetSessionById(character.AccountId, out var session))
            return;

        var active = await _characters.GetCurrentCharacterAsync(character.AccountId, cancel);
        if (active?.Id != character.Id)
            return;

        await SendSnapshotAsync(active, session.Channel, cancel);
    }

    private async Task SendSnapshotAsync(
        CharacterSummary character,
        INetChannel channel,
        CancellationToken cancel = default)
    {
        var experience = await GetExperienceAsync(character.Id, cancel);
        if (experience == null)
            return;

        _net.ServerSendMessage(new MsgCharacterExperienceUpdate
        {
            HasCharacter = true,
            CharacterName = character.Name,
            Experience = new Dictionary<ExperienceGroup, long>(experience),
        }, channel);
    }

    private enum ExperienceMutation : byte
    {
        Set,
        Add,
        Remove,
    }
}
