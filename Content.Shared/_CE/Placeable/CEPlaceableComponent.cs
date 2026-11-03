using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Placeable;

/// <summary>
/// Item that places an entity on the clicked tile, using up one item from the stack.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(CEPlaceableSystem))]
public sealed partial class CEPlaceableComponent : Component
{
    [DataField(required: true)]
    public EntProtoId Prototype;

    /// <summary>
    /// Placement DoAfter length. Zero places the entity instantly.
    /// </summary>
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(1);

    [DataField]
    public SoundSpecifier? PlaceSound = new SoundPathSpecifier("/Audio/Effects/woodenclosetclose.ogg",
        AudioParams.Default.WithVariation(0.03f).WithVolume(2f));

    /// <summary>
    /// All of them must pass for the tile to be valid.
    /// </summary>
    [DataField]
    public List<CEPlacementCondition> Conditions = new();

    /// <summary>
    /// Clicking an open tile next to the user targets the same tile one level below.
    /// </summary>
    [DataField]
    public bool AllowPlaceBelow;

    /// <summary>
    /// Examine line naming the placed entity through a <c>name</c> argument.
    /// </summary>
    [DataField]
    public LocId? ExamineText;

    /// <summary>
    /// Whether the user can turn the preview; otherwise the entity always faces south.
    /// </summary>
    [DataField]
    public bool Rotatable = true;

    /// <summary>
    /// Grid-local direction the placed entity will face.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Direction Direction = Direction.South;
}
