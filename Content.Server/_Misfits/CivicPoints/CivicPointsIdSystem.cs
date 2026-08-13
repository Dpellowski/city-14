using Content.Server._Misfits.Characters;
using Content.Server.Access.Systems;
using Content.Shared._Misfits.CivicPoints;
using Content.Shared.GameTicking;
using Content.Shared.Inventory.Events;
using Robust.Server.Player;
using Robust.Shared.Player;

namespace Content.Server._Misfits.CivicPoints;

/// <summary>
/// Publishes only the derived civic title to the active character's physical ID.
/// </summary>
public sealed partial class CivicPointsIdSystem : EntitySystem
{
    [Dependency] private IPermanentCharacterManager _characters = default!;
    [Dependency] private ICivicPointsManager _civicPoints = default!;
    [Dependency] private IdCardSystem _idCards = default!;
    [Dependency] private IPlayerManager _players = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
        SubscribeLocalEvent<ActorComponent, DidEquipEvent>(OnDidEquip);
        _civicPoints.PointsChanged += OnPointsChanged;
    }

    public override void Shutdown()
    {
        _civicPoints.PointsChanged -= OnPointsChanged;
        base.Shutdown();
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        RefreshId(args.Player, args.Mob);
    }

    private void OnDidEquip(Entity<ActorComponent> ent, ref DidEquipEvent args)
    {
        if (args.Slot != "id")
            return;

        RefreshId(ent.Comp.PlayerSession, ent.Owner);
    }

    private async void OnPointsChanged(CivicPointsChange change)
    {
        if (!_players.TryGetSessionById(change.Character.AccountId, out var session) ||
            session.AttachedEntity is not { } player)
        {
            return;
        }

        var active = await _characters.GetCurrentCharacterAsync(change.Character.AccountId);
        if (active?.Id != change.Character.Id)
            return;

        SetIdPriority(player, change.NewPriority);
    }

    private async void RefreshId(ICommonSession session, EntityUid player)
    {
        var points = await _civicPoints.GetPointsAsync(session);
        if (points == null || Deleted(player))
            return;

        SetIdPriority(player, CivicPointsRules.GetPriority(points.Value));
    }

    private void SetIdPriority(EntityUid player, CivicPriority priority)
    {
        if (!_idCards.TryFindIdCard(player, out var idCard))
            return;

        var civic = EnsureComp<CivicPriorityIdCardComponent>(idCard);
        if (civic.Priority == priority)
            return;

        civic.Priority = priority;
        Dirty(idCard, civic);
    }
}
