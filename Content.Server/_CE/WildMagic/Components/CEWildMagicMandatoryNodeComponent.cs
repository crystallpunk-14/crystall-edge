namespace Content.Server._CE.WildMagic.Components;

/// <summary>
/// Marks a wild magic node as part of the station's maintained pool - when an entity with this
/// component is removed, <see cref="CEWildMagicSystem"/> generates a replacement node on the
/// station to keep the pool's count constant.
/// </summary>
[RegisterComponent]
public sealed partial class CEWildMagicMandatoryNodeComponent : Component;
