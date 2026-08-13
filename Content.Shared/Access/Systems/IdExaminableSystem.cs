using Content.Shared.Access.Components;
using Content.Shared._Misfits.CivicPoints;
using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared.Access.Systems;

public sealed partial class IdExaminableSystem : EntitySystem
{
    [Dependency] private ExamineSystemShared _examineSystem = default!;
    [Dependency] private InventorySystem _inventorySystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<IdExaminableComponent, GetVerbsEvent<ExamineVerb>>(OnGetExamineVerbs);
    }

    private void OnGetExamineVerbs(EntityUid uid, IdExaminableComponent component, GetVerbsEvent<ExamineVerb> args)
    {
        var detailsRange = _examineSystem.IsInDetailsRange(args.User, uid);
        var info = GetMessage(uid);

        var verb = new ExamineVerb()
        {
            Act = () =>
            {
                var markup = FormattedMessage.FromMarkupOrThrow(info);

                _examineSystem.SendExamineTooltip(args.User, uid, markup, false, false);
            },
            Text = Loc.GetString("id-examinable-component-verb-text"),
            Category = VerbCategory.Examine,
            Disabled = !detailsRange,
            Message = detailsRange ? null : Loc.GetString("id-examinable-component-verb-disabled"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/character.svg.192dpi.png"))
        };

        args.Verbs.Add(verb);
    }

    public string GetMessage(EntityUid uid)
    {
        return GetInfo(uid) ?? Loc.GetString("id-examinable-component-verb-no-id");
    }

    public string? GetInfo(EntityUid uid)
    {
        if (_inventorySystem.TryGetSlotEntity(uid, "id", out var idUid))
        {
            // PDA
            if (TryComp(idUid, out PdaComponent? pda) &&
                TryComp<IdCardComponent>(pda.ContainedId, out var id))
            {
                return GetNameJobAndPriority(new Entity<IdCardComponent>(pda.ContainedId.Value, id));
            }
            // ID Card
            if (TryComp(idUid, out id))
            {
                return GetNameJobAndPriority(new Entity<IdCardComponent>(idUid.Value, id));
            }
        }
        return null;
    }

    private string GetNameJobAndPriority(Entity<IdCardComponent> id)
    {
        var jobSuffix = string.IsNullOrWhiteSpace(id.Comp.LocalizedJobTitle) ? string.Empty : $" ({id.Comp.LocalizedJobTitle})";

        var val = string.IsNullOrWhiteSpace(id.Comp.FullName)
            ? Loc.GetString(id.Comp.NameLocId,
                ("jobSuffix", jobSuffix))
            : Loc.GetString(id.Comp.FullNameLocId,
                ("fullName", id.Comp.FullName),
                ("jobSuffix", jobSuffix));

        if (TryComp<CivicPriorityIdCardComponent>(id, out var civic))
        {
            val += "\n" + Loc.GetString("civic-priority-id-examine",
                ("priority", Loc.GetString(civic.Priority.TitleLocId())));
        }

        return val;
    }
}
