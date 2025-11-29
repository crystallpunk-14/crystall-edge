using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._CE.Vampire.Components;

[RegisterComponent]
[NetworkedComponent]
[Access(typeof(CESharedVampireSystem))]
public sealed partial class CEVampireTreeCollectableComponent : Component
{
    [DataField]
    public FixedPoint2 Essence = 1f;

    [DataField]
    public SoundSpecifier CollectSound = new SoundPathSpecifier("/Audio/_CE/Effects/essence_consume.ogg");
}
