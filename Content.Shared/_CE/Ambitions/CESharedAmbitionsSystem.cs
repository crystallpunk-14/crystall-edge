using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.Ambitions;

public abstract class CESharedAmbitionsSystem : EntitySystem
{
}


public sealed partial class CEToggleAmbitionsScreenEvent : InstantActionEvent;


[NetSerializable, Serializable]
public enum CEAmbitionsUIKey : byte
{
    Key
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
public sealed class CEAmbitionDeleteMessage(NetEntity ambition) : BoundUserInterfaceMessage
{
    public readonly NetEntity Ambition = ambition;
}

[Serializable, NetSerializable]
public sealed class CEAmbitionRerollMessage(string title) : BoundUserInterfaceMessage
{
    public readonly string Title = title;
}
