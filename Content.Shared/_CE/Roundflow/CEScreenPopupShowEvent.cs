using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.Roundflow;

[Serializable, NetSerializable]
public sealed class CEScreenPopupShowEvent(string title, string reason = "", SoundSpecifier? audioPath = null)
    : EntityEventArgs
{
    public readonly string Title = title;
    public readonly string Reason = reason;
    public readonly SoundSpecifier? Sound = audioPath;
}
