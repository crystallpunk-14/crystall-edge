using Content.Server._CE.MagicEssence.Systems;

namespace Content.Server._CE.MagicEssence.Components;

/// <summary>
/// Marks a magic essence node as part of the station's maintained pool - when an entity with this
/// component is removed, <see cref="CEMagicEssenceNodeRuleSystem"/> generates a replacement node on
/// the station to keep the pool's count constant.
/// </summary>
[RegisterComponent]
public sealed partial class CEMagicEssenceMandatoryNodeComponent : Component;
