using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Content.Server._Misfits.Characters;
using Content.Server._Misfits.CivicPoints;
using Content.Server.Database._Misfits.CivicPoints;
using Content.Shared._Misfits.CivicPoints;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database;

public abstract partial class ServerDbBase
{
    public async Task<long?> GetCharacterCivicPointsAsync(
        CharacterId characterId,
        CancellationToken cancel = default)
    {
        await using var db = await GetDb(cancel);
        if (!await db.DbContext.Profile.AnyAsync(profile => profile.Id == characterId.Value, cancel))
            return null;

        var balance = await db.DbContext.Set<CharacterCivicPointsModel>()
            .SingleOrDefaultAsync(points => points.ProfileId == characterId.Value, cancel);

        if (balance != null)
            return balance.Points;

        balance = new CharacterCivicPointsModel
        {
            ProfileId = characterId.Value,
            Points = CivicPointsRules.StartingPoints,
        };
        db.DbContext.Add(balance);
        await db.DbContext.SaveChangesAsync(cancel);
        return balance.Points;
    }

    public async Task<CivicPointsDbChange?> MutateCharacterCivicPointsAsync(
        CharacterId characterId,
        CivicPointsMutation mutation,
        long value,
        CivicPointsSource source,
        CancellationToken cancel = default)
    {
        await using var db = await GetDb(cancel);
        await using var transaction = await db.DbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancel);

        if (!await db.DbContext.Profile.AnyAsync(profile => profile.Id == characterId.Value, cancel))
            return null;

        var priorChange = await db.DbContext.Set<CharacterCivicPointChangeModel>()
            .SingleOrDefaultAsync(change => change.ProfileId == characterId.Value &&
                                            change.SourceKind == source.Kind &&
                                            change.SourceId == source.Id,
                cancel);

        if (priorChange != null)
        {
            var matches = mutation switch
            {
                CivicPointsMutation.Delta => priorChange.Delta == value,
                CivicPointsMutation.Set => priorChange.BalanceAfter == value,
                _ => throw new ArgumentOutOfRangeException(nameof(mutation), mutation, null),
            };

            if (!matches)
            {
                throw new InvalidOperationException(
                    "A Civic Points source ID was reused with a different mutation.");
            }

            var priorBalance = checked(priorChange.BalanceAfter - priorChange.Delta);
            await transaction.CommitAsync(cancel);
            return new CivicPointsDbChange(
                priorBalance,
                priorChange.BalanceAfter,
                priorChange.Delta,
                false);
        }

        var balance = await db.DbContext.Set<CharacterCivicPointsModel>()
            .SingleOrDefaultAsync(points => points.ProfileId == characterId.Value, cancel);

        if (balance == null)
        {
            balance = new CharacterCivicPointsModel
            {
                ProfileId = characterId.Value,
                Points = CivicPointsRules.StartingPoints,
            };
            db.DbContext.Add(balance);
        }

        var previous = balance.Points;
        var next = mutation switch
        {
            CivicPointsMutation.Delta => checked(previous + value),
            CivicPointsMutation.Set => value,
            _ => throw new ArgumentOutOfRangeException(nameof(mutation), mutation, null),
        };
        var delta = checked(next - previous);

        balance.Points = next;
        db.DbContext.Add(new CharacterCivicPointChangeModel
        {
            ProfileId = characterId.Value,
            Delta = delta,
            BalanceAfter = next,
            SourceKind = source.Kind,
            SourceId = source.Id,
            Reason = source.Reason,
            CreatedAt = DateTimeOffset.UtcNow,
        });

        await db.DbContext.SaveChangesAsync(cancel);
        await transaction.CommitAsync(cancel);
        return new CivicPointsDbChange(previous, next, delta, true);
    }

    private static async Task EnsureCharacterCivicPointsAsync(
        ServerDbContext db,
        int profileId,
        CancellationToken cancel = default)
    {
        if (await db.Set<CharacterCivicPointsModel>().AnyAsync(points => points.ProfileId == profileId, cancel))
            return;

        db.Add(new CharacterCivicPointsModel
        {
            ProfileId = profileId,
            Points = CivicPointsRules.StartingPoints,
        });
        await db.SaveChangesAsync(cancel);
    }
}
