using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.Ambitions;

public abstract class CESharedAmbitionsSystem : EntitySystem;

[NetSerializable, Serializable]
public sealed partial class CEToggleAmbitionsScreenEvent : EntityEventArgs;

[NetSerializable, Serializable]
public enum CEAmbitionsUIKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class CEAmbitionsBuiState(List<(string, string)> ambitions, int rerolls, int maxAmbitions) : BoundUserInterfaceState
{
    public List<(string, string)> Ambitions = ambitions;
    public int Rerolls = rerolls;
    public int MaxAmbitions = maxAmbitions;
}

[Serializable, NetSerializable]
public sealed class CEAmbitionCreateMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class CEAmbitionLockMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class CEAmbitionDeleteMessage(int index) : BoundUserInterfaceMessage
{
    public readonly int Index = index;
}
