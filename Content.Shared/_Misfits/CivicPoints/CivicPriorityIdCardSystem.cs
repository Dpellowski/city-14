using Content.Shared.Examine;

namespace Content.Shared._Misfits.CivicPoints;

public sealed class CivicPriorityIdCardSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CivicPriorityIdCardComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<CivicPriorityIdCardComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("civic-priority-id-examine",
            ("priority", Loc.GetString(ent.Comp.Priority.TitleLocId()))));
    }
}
