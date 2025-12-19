using Content.Shared.Mind;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.BlueText;

public abstract class CESharedBlueTextSystem : EntitySystem
{
    public const int MaxTextLength = 1000;

    [Dependency] protected readonly SharedMindSystem Mind = default!;
}


[NetSerializable, Serializable]
public sealed partial class CEToggleBlueTextScreenEvent : EntityEventArgs;

[NetSerializable, Serializable]
public enum CEBlueTextUIKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class CEBlueTextBuiState(string text) : BoundUserInterfaceState
{
    public string Text = text;
}

[Serializable, NetSerializable]
public sealed class CEBlueTextSubmitMessage(string text) : BoundUserInterfaceMessage
{
    public string Text = text;
}
