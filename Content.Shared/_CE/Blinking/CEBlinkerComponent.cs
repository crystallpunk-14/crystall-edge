/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 *
 * Taken from https://github.com/EphemeralSpace/ephemeral-space/pull/335/files?notification_referrer_id=NT_kwDOBb-lNbQyMDgzMjQ4Nzk4Nzo5NjQ0NTc0OQ
 */

using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._CE.Blinking;

/// <summary>
/// Makes a character blink. That's it.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(CESharedBlinkingSystem))]
public sealed partial class CEBlinkerComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextBlinkTime;

    [DataField]
    public TimeSpan MinBlinkDelay = TimeSpan.FromSeconds(2);

    [DataField]
    public TimeSpan MaxBlinkDelay = TimeSpan.FromSeconds(10);

    [DataField, AutoNetworkedField]
    public bool Enabled = true;
}

[Serializable, NetSerializable]
public enum CEBlinkVisuals : byte
{
    EyesClosed,
}
