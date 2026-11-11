using Content.Client.Overlays;
using Content.Shared._CE.DayCycle;

namespace Content.Client._CE.DayCycle;

/// <summary>
/// Active while the local player wears a clock or has the time sense status effect; read by the clock widget
/// </summary>
public sealed partial class CETimeSenseHudSystem : EquipmentHudSystem<CETimeSenseComponent>
{
}
