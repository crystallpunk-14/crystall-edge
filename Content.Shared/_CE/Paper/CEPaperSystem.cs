using Content.Shared._CE.Pen;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.UserInterface;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._CE.Paper;

/// <summary>
/// Offers and performs the "write" pen action on behalf of <see cref="PaperComponent"/>, kept
/// separate from the vanilla <see cref="PaperSystem"/> to avoid touching upstream code.
/// </summary>
public sealed partial class CEPaperSystem : EntitySystem
{
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    private static readonly ProtoId<TagPrototype> WriteIgnoreStampsTag = "WriteIgnoreStamps";

    private static readonly SpriteSpecifier WriteIcon =
        new SpriteSpecifier.Rsi(new ResPath("/Textures/_CE/Interface/Paper/pen_interact_icons.rsi"), "write");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PaperComponent, CEGetPenActionsEvent>(OnGetPenActions);
    }

    private void OnGetPenActions(Entity<PaperComponent> entity, ref CEGetPenActionsEvent args)
    {
        if (!CanWrite(entity, args.Pen))
            return;

        args.Actions.Add(new CEPenAction(CEPenActionKind.Write, "ce-pen-action-write", WriteIcon));
    }

    private bool CanWrite(Entity<PaperComponent> entity, EntityUid pen)
    {
        var editable = entity.Comp.StampedBy.Count == 0 || _tag.HasTag(pen, WriteIgnoreStampsTag);
        return editable && !entity.Comp.EditingDisabled;
    }

    /// <summary>
    /// Opens the standard paper-writing UI, mirroring what <see cref="PaperSystem"/> does when a
    /// vanilla <c>Write</c>-tagged pen is used on paper directly.
    /// </summary>
    public bool TryWrite(EntityUid target, EntityUid user, EntityUid pen)
    {
        if (!TryComp<PaperComponent>(target, out var paper) || !CanWrite((target, paper), pen))
            return false;

        var attemptEv = new PaperWriteAttemptEvent(target);
        RaiseLocalEvent(user, ref attemptEv);
        if (attemptEv.Cancelled)
        {
            if (attemptEv.FailReason is not null)
                _popup.PopupClient(Loc.GetString(attemptEv.FailReason), target, user);
            return false;
        }

        var writeEvent = new PaperWriteEvent(user, target);
        RaiseLocalEvent(pen, ref writeEvent);

        paper.Mode = PaperComponent.PaperAction.Write;
        _ui.OpenUi(target, PaperComponent.PaperUiKey.Key, user);
        _ui.SetUiState(target, PaperComponent.PaperUiKey.Key,
            new PaperComponent.PaperBoundUserInterfaceState(paper.Content, paper.StampedBy, paper.Mode));
        return true;
    }
}
