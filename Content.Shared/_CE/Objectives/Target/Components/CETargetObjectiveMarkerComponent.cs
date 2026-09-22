namespace Content.Shared._CE.Objectives.Target.Components;

/// <summary>
/// Marker component added to an entity that is the target of one or more
/// <see cref="CETargetObjectiveComponent"/> objectives, tracking which ones so they can be updated
/// (e.g. when the target dies or its mind changes).
/// </summary>
[RegisterComponent]
[Access(typeof(CETargetObjectiveSystem), Other = AccessPermissions.None)]
public sealed partial class CETargetObjectiveMarkerComponent : Component
{
    [DataField]
    public HashSet<EntityUid> Objectives = new();
}
