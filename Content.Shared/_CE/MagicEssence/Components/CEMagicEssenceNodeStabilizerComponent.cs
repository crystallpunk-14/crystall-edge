using Robust.Shared.GameStates;

namespace Content.Shared._CE.MagicEssence.Components;

/// <summary>
/// Marks an entity as a node stabilizer sphere: while anchored and powered, it looks for a
/// <see cref="CEMagicEssenceNodeComponent"/> on the same tile and stops that node's aging entirely
/// (no fade, no despawn - it keeps generating essence as normal) for as long as it stays anchored
/// and powered - see the server-side magic essence node stabilizer system.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CEMagicEssenceNodeStabilizerComponent : Component;
