using Content.Shared._CE.GOAP.Components;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Utility;

namespace Content.Client._CE.GOAP;

/// <summary>
/// Displays the sleeping (zzz) status icon above GOAP entities that have
/// <see cref="CEGOAPSleepingComponent"/>. Uses the StatusIcon overlay system,
/// same mechanism as the SSD indicator.
/// </summary>
public sealed class CEGOAPSleepingVisualSystem : EntitySystem
{
    private StatusIconData _sleepIcon = default!;

    public override void Initialize()
    {
        base.Initialize();

        _sleepIcon = new StatusIconData
        {
            Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Effects/ssd.rsi"), "default0"),
            LocationPreference = StatusIconLocationPreference.Left,
        };

        SubscribeLocalEvent<CEGOAPSleepingComponent, GetStatusIconsEvent>(OnGetStatusIcon);
    }

    private void OnGetStatusIcon(EntityUid uid, CEGOAPSleepingComponent component, ref GetStatusIconsEvent args)
    {
        args.StatusIcons.Add(_sleepIcon);
    }
}
