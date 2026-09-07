using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.DimensionalLift;

/// <summary>
/// A structure that opens a linked pair of <see cref="Content.Shared.Teleportation.Components.PortalComponent"/>
/// portals along the z-level stack: the first portal sits on the lift itself, the second is placed on the nearest
/// z-level below that has a free floor tile directly under the lift. Meant for a flying island punching a hole down
/// to the surface, or for reaching into a roofed room from the level above.
///
/// Portals are (re)built whenever the map z-network is rebuilt, and — if the lift has an
/// <see cref="Content.Server.Power.Components.ApcPowerReceiverComponent"/> — whenever its power state changes.
/// A lift without a power receiver is always active. Losing power closes the portals.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CEDimensionalLiftComponent : Component
{
    /// <summary>
    /// Portal spawned on the lift entity itself.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? FirstPortal;

    /// <summary>
    /// Portal spawned on the level below.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? SecondPortal;

    [DataField]
    public EntProtoId FirstPortalPrototype = "CEPortalDimensionalLift";

    [DataField]
    public EntProtoId SecondPortalPrototype = "CEPortalDimensionalLift";

    /// <summary>
    /// How many z-levels down to scan for a landing spot before giving up.
    /// </summary>
    [DataField]
    public int MaxSearchDepth = 16;

    [DataField]
    public SoundSpecifier OpenSound =
        new SoundPathSpecifier("/Audio/Machines/high_tech_confirm.ogg")
        {
            Params = AudioParams.Default.AddVolume(-2f),
        };

    [DataField]
    public SoundSpecifier CloseSound = new SoundPathSpecifier("/Audio/Machines/button.ogg");
}
