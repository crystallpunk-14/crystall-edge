using Content.Client._CE.Animation.Core;
using Content.Shared._CE.EntityEffect.Effects;
using Robust.Client.Graphics;
using Robust.Shared.Console;

namespace Content.Client._CE.MeleeWeapon;

/// <summary>
/// Client-side system that listens for <see cref="CEDebugArcAttackEvent"/> and feeds
/// debug arc data to <see cref="CEMeleeArcOverlay"/> for visualization.
/// </summary>
public sealed partial class CEArcAttackDebugSystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlay = default!;

    private CEMeleeArcOverlay? _activeOverlay;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CEDebugArcAttackEvent>(OnArcAttackFired);
    }

    private void OnArcAttackFired(CEDebugArcAttackEvent ev)
    {
        _activeOverlay?.AddArc(ev.Position, ev.Direction, ev.Range, ev.ArcWidth);
    }

    public void Toggle()
    {
        if (_activeOverlay != null && _overlay.RemoveOverlay(_activeOverlay))
        {
            _activeOverlay = null;
            return;
        }

        _activeOverlay = new CEMeleeArcOverlay();
        _overlay.AddOverlay(_activeOverlay);
    }
}

/// <summary>
/// Console command to toggle the CEWeapon ArcAttack debug overlay.
/// Replaces vanilla melee's "showmeleespread" (see removed Content.Client/Weapons/Melee/MeleeSpreadCommand.cs)
/// now that _CE prototypes attack through CEWeapon instead of vanilla MeleeWeapon.
/// Usage: showmeleespread
/// </summary>
public sealed partial class CEShowArcAttackCommand : LocalizedCommands
{
    [Dependency] private IEntitySystemManager _systemManager = default!;

    public override string Command => "showmeleespread";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        _systemManager.GetEntitySystem<CEArcAttackDebugSystem>().Toggle();
    }
}
