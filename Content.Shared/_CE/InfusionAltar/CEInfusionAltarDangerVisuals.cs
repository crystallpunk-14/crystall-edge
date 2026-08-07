using Robust.Shared.Serialization;

namespace Content.Shared._CE.InfusionAltar;

[Serializable, NetSerializable]
public enum CEInfusionAltarDangerVisuals : byte
{
    Level,
}

[Serializable, NetSerializable]
public enum CEInfusionAltarDangerLevel : byte
{
    Calm,
    Unstable,
    Critical,
}
