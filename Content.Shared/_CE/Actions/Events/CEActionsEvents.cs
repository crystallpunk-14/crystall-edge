using Content.Shared.Actions;

namespace Content.Shared._CE.Actions.Events;

public interface ICEMagicEffect
{
    public TimeSpan Cooldown { get; }
}

public sealed partial class CEWorldTargetActionEvent : WorldTargetActionEvent, ICEMagicEffect
{
    [DataField]
    public TimeSpan Cooldown { get; private set; } = TimeSpan.FromSeconds(1f);
}

public sealed partial class CEEntityTargetActionEvent : EntityTargetActionEvent, ICEMagicEffect
{
    [DataField]
    public TimeSpan Cooldown { get; private set; } = TimeSpan.FromSeconds(1f);
}

public sealed partial class CEInstantActionEvent : InstantActionEvent, ICEMagicEffect
{
    [DataField]
    public TimeSpan Cooldown { get; private set; } = TimeSpan.FromSeconds(1f);
}

