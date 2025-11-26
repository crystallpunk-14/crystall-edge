using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.Ambitions;

public abstract class CESharedAmbitionsSystem : EntitySystem;

public sealed partial class CEToggleAmbitionsScreenEvent : InstantActionEvent;

[NetSerializable, Serializable]
public enum CEAmbitionsUIKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class CEAmbitionsBuiState(List<(string, string)> ambitions, int rerolls, int maxAmbitions, TimeSpan endTime, TimeSpan maxTime) : BoundUserInterfaceState
{
    public List<(string, string)> Ambitions = ambitions;
    public int Rerolls = rerolls;
    public int MaxAmbitions = maxAmbitions;
    public TimeSpan EndTime = endTime;
    public TimeSpan MaxTime = maxTime;
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
