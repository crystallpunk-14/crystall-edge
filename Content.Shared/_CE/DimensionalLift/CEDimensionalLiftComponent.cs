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

    /// <summary>
    /// Purely cosmetic "beam" spawned on every z-level the rift crosses, including the levels the portals
    /// stand on. Respawned alongside the portals and cleared when they close.
    /// </summary>
    [DataField]
    public EntProtoId TraversalEffectPrototype = "CEDimensionalLiftTraversalEffect";

    /// <summary>
    /// Currently spawned traversal effect entities, one per level crossed.
    /// </summary>
    [DataField]
    public List<EntityUid> TraversalEffects = new();

    /// <summary>
    /// Brief purple flash spawned alongside the beam at each crossed level. Self-despawns, so it isn't tracked.
    /// </summary>
    [DataField]
    public EntProtoId TraversalImpactPrototype = "CEDimensionalLiftTraversalImpact";

    [DataField]
    public SoundSpecifier OpenSound =
        new SoundPathSpecifier("/Audio/Magic/Eldritch/voidblink.ogg")
        {
            Params = AudioParams.Default.AddVolume(-2f),
        };

    [DataField]
    public SoundSpecifier CloseSound = new SoundPathSpecifier("/Audio/Machines/button.ogg");
}
