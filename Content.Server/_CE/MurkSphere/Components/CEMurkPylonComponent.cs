using Content.Server._CE.MurkSphere;

namespace Content.Server._CE.MurkSphere.Components;

/// <summary>
/// Marks an entity as one of the Light Monolith's pylons - see <see cref="CEMurkPylonSystem"/>
/// and <see cref="CEMurkSphereFixerComponent.PylonsRequired"/>.
/// </summary>
[RegisterComponent, Access(typeof(CEMurkPylonSystem))]
public sealed partial class CEMurkPylonComponent : Component;
