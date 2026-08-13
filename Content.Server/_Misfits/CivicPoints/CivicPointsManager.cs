using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Content.Server._Misfits.Characters;
using Content.Server.Database;
using Content.Shared._Misfits.CivicPoints;
using Robust.Server.Player;
using Robust.Shared.Player;

namespace Content.Server._Misfits.CivicPoints;

public sealed partial class CivicPointsManager : ICivicPointsManager
{
    [Dependency] private IPermanentCharacterManager _characters = default!;
    [Dependency] private IServerDbManager _db = default!;
    [Dependency] private ILogManager _logs = default!;
    [Dependency] private IPlayerManager _players = default!;

    private readonly ConcurrentDictionary<CharacterId, SemaphoreSlim> _mutationLocks = new();
    private ISawmill? _sawmill;

    public event Action<CivicPointsChange>? PointsChanged;

    public Task<long?> GetPointsAsync(
        CharacterId characterId,
        CancellationToken cancel = default)
    {
        return _db.GetCharacterCivicPointsAsync(characterId, cancel);
    }

    public async Task<long?> GetPointsAsync(
        ICommonSession session,
        CancellationToken cancel = default)
    {
        var character = await _characters.GetCurrentCharacterAsync(session.UserId, cancel);
        return character == null
            ? null
            : await GetPointsAsync(character.Id, cancel);
    }

    public Task<CivicPointsChange?> AddPointsAsync(
        CharacterId characterId,
        long amount,
        CivicPointsSource source,
        CancellationToken cancel = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        return MutateAsync(characterId, CivicPointsMutation.Delta, amount, source, cancel);
    }

    public Task<CivicPointsChange?> RemovePointsAsync(
        CharacterId characterId,
        long amount,
        CivicPointsSource source,
        CancellationToken cancel = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        return MutateAsync(characterId, CivicPointsMutation.Delta, checked(-amount), source, cancel);
    }

    public Task<CivicPointsChange?> SetPointsAsync(
        CharacterId characterId,
        long points,
        CivicPointsSource source,
        CancellationToken cancel = default)
    {
        return MutateAsync(characterId, CivicPointsMutation.Set, points, source, cancel);
    }

    public async Task<CivicPointsChange?> AddPointsAsync(
        ICommonSession session,
        long amount,
        CivicPointsSource source,
        CancellationToken cancel = default)
    {
        var character = await _characters.GetCurrentCharacterAsync(session.UserId, cancel);
        return character == null
            ? null
            : await AddPointsAsync(character.Id, amount, source, cancel);
    }

    public async Task<CivicPointsChange?> RemovePointsAsync(
        ICommonSession session,
        long amount,
        CivicPointsSource source,
        CancellationToken cancel = default)
    {
        var character = await _characters.GetCurrentCharacterAsync(session.UserId, cancel);
        return character == null
            ? null
            : await RemovePointsAsync(character.Id, amount, source, cancel);
    }

    public Task<CivicPointsChange?> AddPointsAsync(
        EntityUid player,
        long amount,
        CivicPointsSource source,
        CancellationToken cancel = default)
    {
        if (!_players.TryGetSessionByEntity(player, out var session))
            return Task.FromResult<CivicPointsChange?>(null);

        return AddPointsAsync(session, amount, source, cancel);
    }

    public Task<CivicPointsChange?> RemovePointsAsync(
        EntityUid player,
        long amount,
        CivicPointsSource source,
        CancellationToken cancel = default)
    {
        if (!_players.TryGetSessionByEntity(player, out var session))
            return Task.FromResult<CivicPointsChange?>(null);

        return RemovePointsAsync(session, amount, source, cancel);
    }

    private async Task<CivicPointsChange?> MutateAsync(
        CharacterId characterId,
        CivicPointsMutation mutation,
        long value,
        CivicPointsSource source,
        CancellationToken cancel)
    {
        ValidateSource(source);

        var gate = _mutationLocks.GetOrAdd(characterId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancel);

        CivicPointsChange? change;
        try
        {
            var character = await _characters.GetCharacterAsync(characterId, cancel);
            if (character == null)
                return null;

            var result = await _db.MutateCharacterCivicPointsAsync(
                characterId,
                mutation,
                value,
                source,
                cancel);

            if (result == null)
                return null;

            change = new CivicPointsChange(
                character,
                result.PreviousPoints,
                result.NewPoints,
                result.Delta,
                CivicPointsRules.GetPriority(result.PreviousPoints),
                CivicPointsRules.GetPriority(result.NewPoints),
                source,
                result.Applied);
        }
        finally
        {
            gate.Release();
        }

        if (change.Applied)
            PublishChange(change);

        return change;
    }

    private static void ValidateSource(CivicPointsSource source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source.Kind);
        ArgumentException.ThrowIfNullOrWhiteSpace(source.Id);
        ArgumentException.ThrowIfNullOrWhiteSpace(source.Reason);

        if (source.Kind.Length > 32)
            throw new ArgumentOutOfRangeException(nameof(source), "Source kind cannot exceed 32 characters.");
        if (source.Id.Length > 128)
            throw new ArgumentOutOfRangeException(nameof(source), "Source ID cannot exceed 128 characters.");
        if (source.Reason.Length > 512)
            throw new ArgumentOutOfRangeException(nameof(source), "Reason cannot exceed 512 characters.");
    }

    private void PublishChange(CivicPointsChange change)
    {
        if (PointsChanged == null)
            return;

        _sawmill ??= _logs.GetSawmill("misfits.civic_points");
        foreach (Action<CivicPointsChange> subscriber in PointsChanged.GetInvocationList())
        {
            try
            {
                subscriber(change);
            }
            catch (Exception exception)
            {
                _sawmill.Error("Civic Points change subscriber failed: {Exception}", exception);
            }
        }
    }
}
