using Content.Shared._CE.MagicEssence.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.MagicEssence.Components;

/// <summary>
/// Marks the singleton entity holding the round-cached thaumaturgical essence composition of
/// every <see cref="EntProtoId"/> that has been rolled so far. Spawned lazily by
/// <see cref="Systems.CEMagicEssenceSystem"/> the first time an essence calculation is requested.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CEMagicEssenceSingletonComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<EntProtoId, Dictionary<ProtoId<CEMagicEssenceTypePrototype>, int>> Cache = new();
}
