using Content.Shared._CE.MagicFocus.Components;
using Content.Shared.Examine;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._CE.MagicFocus.Systems;

public sealed partial class CEMagicFocusSystem
{
    [Dependency] private ExamineSystemShared _examine = default!;

    private void InitExamine()
    {
        SubscribeLocalEvent<CEMagicFocusComponent, GetVerbsEvent<ExamineVerb>>(OnFocusVerbExamine);
    }

    private void OnFocusVerbExamine(Entity<CEMagicFocusComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var examineMarkup = GetFocusExamine(ent.Comp);

        _examine.AddDetailedExamineVerb(args, ent.Comp, examineMarkup,
            Loc.GetString("ce-magic-focus-examinable-verb-text"), "/Textures/Interface/VerbIcons/dot.svg.192dpi.png",
            Loc.GetString("ce-magic-focus-examinable-verb-message"));
    }

    private FormattedMessage GetFocusExamine(CEMagicFocusComponent comp)
    {
        var msg = new FormattedMessage();
        msg.AddMarkupOrThrow(Loc.GetString("ce-magic-focus-examine-header"));

        foreach (var (type, cap) in comp.Volume)
        {
            if (!_proto.TryIndex(type, out var essenceType))
                continue;

            msg.PushNewline();
            msg.AddMarkupOrThrow(Loc.GetString("ce-magic-focus-examine-type-cap", ("type", essenceType.Name), ("cap", cap)));
        }

        msg.PushNewline();
        var defaultCapKey = comp.Volume.Count == 0 ? "ce-magic-focus-examine-default-cap-all" : "ce-magic-focus-examine-default-cap";
        msg.AddMarkupOrThrow(Loc.GetString(defaultCapKey, ("cap", comp.MinimumVolume)));

        msg.PushNewline();
        msg.PushNewline();
        msg.AddMarkupOrThrow(Loc.GetString("ce-magic-focus-examine-max-types", ("max", comp.MaxEssenceTypes)));

        return msg;
    }
}
