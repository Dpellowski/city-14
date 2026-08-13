using System.Threading;
using System.Threading.Tasks;
using Content.Server._Misfits.Characters;
using Content.Shared._Misfits.CivicPoints;
using Robust.Shared.Player;

namespace Content.Server._Misfits.CivicPoints;

public sealed record CivicPointsSource(
    string Kind,
    string Id,
    string Reason)
{
    public const string TaskKind = "task";
    public const string AdminKind = "admin";
    public const string SystemKind = "system";

    public static CivicPointsSource Task(string taskInstanceId, string transition, string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskInstanceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(transition);

        return new CivicPointsSource(TaskKind, $"{taskInstanceId}:{transition}", reason);
    }

    public static CivicPointsSource Admin(string reason)
    {
        return new CivicPointsSource(AdminKind, Guid.NewGuid().ToString("N"), reason);
    }
}

public enum CivicPointsMutation : byte
{
    Delta,
    Set,
}

public sealed record CivicPointsDbChange(
    long PreviousPoints,
    long NewPoints,
    long Delta,
    bool Applied);

public sealed record CivicPointsChange(
    CharacterSummary Character,
    long PreviousPoints,
    long NewPoints,
    long Delta,
    CivicPriority PreviousPriority,
    CivicPriority NewPriority,
    CivicPointsSource Source,
    bool Applied);

public interface ICivicPointsDbManager
{
    Task<long?> GetCharacterCivicPointsAsync(
        CharacterId characterId,
        CancellationToken cancel = default);

    Task<CivicPointsDbChange?> MutateCharacterCivicPointsAsync(
        CharacterId characterId,
        CivicPointsMutation mutation,
        long value,
        CivicPointsSource source,
        CancellationToken cancel = default);
}

/// <summary>
/// Server-only API for authoritative task, system, and administrative point changes.
/// </summary>
public interface ICivicPointsManager
{
    event Action<CivicPointsChange>? PointsChanged;

    Task<long?> GetPointsAsync(
        CharacterId characterId,
        CancellationToken cancel = default);

    Task<long?> GetPointsAsync(
        ICommonSession session,
        CancellationToken cancel = default);

    Task<CivicPointsChange?> AddPointsAsync(
        CharacterId characterId,
        long amount,
        CivicPointsSource source,
        CancellationToken cancel = default);

    Task<CivicPointsChange?> RemovePointsAsync(
        CharacterId characterId,
        long amount,
        CivicPointsSource source,
        CancellationToken cancel = default);

    Task<CivicPointsChange?> SetPointsAsync(
        CharacterId characterId,
        long points,
        CivicPointsSource source,
        CancellationToken cancel = default);

    Task<CivicPointsChange?> AddPointsAsync(
        ICommonSession session,
        long amount,
        CivicPointsSource source,
        CancellationToken cancel = default);

    Task<CivicPointsChange?> RemovePointsAsync(
        ICommonSession session,
        long amount,
        CivicPointsSource source,
        CancellationToken cancel = default);

    Task<CivicPointsChange?> AddPointsAsync(
        EntityUid player,
        long amount,
        CivicPointsSource source,
        CancellationToken cancel = default);

    Task<CivicPointsChange?> RemovePointsAsync(
        EntityUid player,
        long amount,
        CivicPointsSource source,
        CancellationToken cancel = default);
}
