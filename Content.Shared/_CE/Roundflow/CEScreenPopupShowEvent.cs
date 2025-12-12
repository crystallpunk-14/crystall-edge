using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.Roundflow;

[Serializable, NetSerializable]
public sealed class CEScreenPopupShowEvent(string title, string desc = "", SoundSpecifier? audioPath = null)
    : EntityEventArgs
{
    public readonly string Title = title;
    public readonly string Description = desc;
    public readonly SoundSpecifier? Sound = audioPath;
}
