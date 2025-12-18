
using Content.Shared.Mind;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.Bluetext;

public abstract class CESharedBlueTextSystem : EntitySystem
{
    [Dependency] protected readonly SharedMindSystem Mind = default!;
}


[NetSerializable, Serializable]
public sealed partial class CEToggleBluetextScreenEvent : EntityEventArgs;

[NetSerializable, Serializable]
public enum CEBluetextUIKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class CEBluetextBuiState(string text) : BoundUserInterfaceState
{
    public string Text = text;
}
