using System.Threading;
using System.Threading.Tasks;

namespace Content.Server._Misfits.CivicPoints;

/// <summary>
/// Integration surface for server-authoritative task systems. Callers must supply a stable task instance ID.
/// </summary>
public interface ICivicPointsTaskRewards
{
    Task<CivicPointsChange?> AwardCompletionAsync(
        EntityUid character,
        string taskInstanceId,
        long amount,
        string reason,
        CancellationToken cancel = default);

    Task<CivicPointsChange?> RemoveForFailureAsync(
        EntityUid character,
        string taskInstanceId,
        long amount,
        string reason,
        CancellationToken cancel = default);

    Task<CivicPointsChange?> ReverseCompletionAsync(
        EntityUid character,
        string taskInstanceId,
        long amount,
        string reason,
        CancellationToken cancel = default);
}

public sealed partial class CivicPointsTaskRewards : ICivicPointsTaskRewards
{
    [Dependency] private ICivicPointsManager _civicPoints = default!;

    public Task<CivicPointsChange?> AwardCompletionAsync(
        EntityUid character,
        string taskInstanceId,
        long amount,
        string reason,
        CancellationToken cancel = default)
    {
        return _civicPoints.AddPointsAsync(
            character,
            amount,
            CivicPointsSource.Task(taskInstanceId, "completed", reason),
            cancel);
    }

    public Task<CivicPointsChange?> RemoveForFailureAsync(
        EntityUid character,
        string taskInstanceId,
        long amount,
        string reason,
        CancellationToken cancel = default)
    {
        return _civicPoints.RemovePointsAsync(
            character,
            amount,
            CivicPointsSource.Task(taskInstanceId, "failed", reason),
            cancel);
    }

    public Task<CivicPointsChange?> ReverseCompletionAsync(
        EntityUid character,
        string taskInstanceId,
        long amount,
        string reason,
        CancellationToken cancel = default)
    {
        return _civicPoints.RemovePointsAsync(
            character,
            amount,
            CivicPointsSource.Task(taskInstanceId, "completion-reversed", reason),
            cancel);
    }
}
