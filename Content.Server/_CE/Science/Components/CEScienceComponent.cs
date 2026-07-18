namespace Content.Server._CE.Science.Components;

/// <summary>
/// Marks the singleton nullspace entity holding round-wide science data.
/// Spawned by <see cref="CEScienceSystem"/> on round start; duplicates are deleted on MapInit.
/// </summary>
[RegisterComponent]
public sealed partial class CEScienceComponent : Component
{
}
