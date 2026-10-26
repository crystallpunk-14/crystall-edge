using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared._DND.Features;

public static class DndActions
{
    public const string Rage = "DndActionRage";
    public const string WeaponDrill = "DndActionWeaponDrill";
    public const string Inspire = "DndActionBardicInspiration";
}

/// <summary>
/// A short rest is one quiet minute. A long rest is ten quiet minutes. Damage interrupts either.
/// Those stand in for an hour and eight hours so a rest can finish during a session.
/// </summary>
public static class DndRest
{
    public static readonly TimeSpan Short = TimeSpan.FromMinutes(1);
    public static readonly TimeSpan Long = TimeSpan.FromMinutes(10);
}

public sealed partial class DndRageActionEvent : InstantActionEvent;

[Serializable, NetSerializable]
public sealed partial class DndShortRestEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class DndLongRestEvent : SimpleDoAfterEvent;

public sealed partial class DndWeaponDrillActionEvent : InstantActionEvent;

public sealed partial class DndInspireEvent : EntityTargetActionEvent;

public sealed partial class DndCastSpellEvent : EntityTargetActionEvent
{
    [DataField(required: true)]
    public string Spell = "";
}
