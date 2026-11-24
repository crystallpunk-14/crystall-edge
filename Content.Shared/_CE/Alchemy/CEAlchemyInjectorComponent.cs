using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Alchemy;

/// <summary>
/// An auto-injector: after a do-after it pours its whole solution into the target's bloodstream.
/// It can be refilled and emptied again (spill) like any container.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(CEAlchemyInjectorSystem))]
public sealed partial class CEAlchemyInjectorComponent : Component
{
    [DataField]
    public string SolutionName = "injector";

    /// <summary>
    /// Do-after time on oneself.
    /// </summary>
    [DataField]
    public TimeSpan SelfDelay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Do-after time on someone else.
    /// </summary>
    [DataField]
    public TimeSpan OtherDelay = TimeSpan.FromSeconds(3);

    [DataField]
    public bool BreakOnMoveSelf;

    [DataField]
    public bool BreakOnMoveOther = true;

    [DataField]
    public SoundSpecifier? BeginSound;

    [DataField]
    public SoundSpecifier? InjectSound = new SoundPathSpecifier("/Audio/Items/hypospray.ogg");

    /// <summary>
    /// The flash that appears on the target, colored like the injected solution.
    /// </summary>
    [DataField]
    public EntProtoId? Vfx = "CEAlchemyTransmuteVFX";
}
