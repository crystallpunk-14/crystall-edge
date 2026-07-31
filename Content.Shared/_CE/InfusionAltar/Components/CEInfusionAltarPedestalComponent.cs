namespace Content.Shared._CE.InfusionAltar.Components;

/// <summary>
/// Marks a sub-pedestal that can be anchored around a <see cref="CEInfusionAltarComponent"/> altar to
/// hold a surrounding ingredient item. Connection to a nearby altar is maintained by
/// <see cref="Content.Server._CE.InfusionAltar.CEInfusionAltarSystem"/>.
/// </summary>
[RegisterComponent]
public sealed partial class CEInfusionAltarPedestalComponent : Component;
