using Robust.Shared.Serialization;

namespace Content.Shared._CE.InfusionAltar;

[Serializable, NetSerializable]
public enum CEInfusionAltarDangerVisuals : byte
{
    Level,

    /// <summary>
    /// Whether instability is above zero - the chaos effect layer is hidden entirely while calm and idle.
    /// </summary>
    Active,
}

[Serializable, NetSerializable]
public enum CEInfusionAltarDangerLevel : byte
{
    Calm,
    Unstable,
    Critical,
}
