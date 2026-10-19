using System.Numerics;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.Murk.Events;

[Serializable, NetSerializable]
public sealed class CEMurkDebugOverlayToggledEvent(bool isEnabled) : EntityEventArgs
{
    public readonly bool IsEnabled = isEnabled;
}

[Serializable, NetSerializable]
public readonly record struct CEMurkDebugSource(NetEntity Map, Vector2 WorldPos, float Strength);

[Serializable, NetSerializable]
public sealed class CEMurkDebugOverlaySnapshotEvent(
    List<CEMurkDebugSource> freeZoneSources,
    List<CEMurkDebugSource> boundarySources) : EntityEventArgs
{
    public readonly List<CEMurkDebugSource> FreeZoneSources = freeZoneSources;
    public readonly List<CEMurkDebugSource> BoundarySources = boundarySources;
}
