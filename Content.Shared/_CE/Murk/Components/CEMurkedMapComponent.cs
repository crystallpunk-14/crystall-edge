using System.Numerics;
using Content.Shared._CE.Murk.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._CE.Murk.Components;

/// <summary>
/// Sets the base murk value on the map, which can be modified by various sources.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class CEMurkedMapComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Intensity = 0f;

    public float LerpedIntensity = 0f;

    //Clientside data for overlay rendering

    /// <summary>
    ///     Maximum number of observers zones that can be shown on screen at a time.
    ///     If this value is changed, the shader itself also needs to be updated.
    /// </summary>
    public static int MaxCount = 64;
    public readonly HashSet<EntityUid> Seen = [];

    public readonly Dictionary<EntityUid, MurkEntry> MurkBuffer = new();
    public readonly Vector2[] Positions = new Vector2[MaxCount];
    public readonly float[] Intensities = new float[MaxCount];
    public int Count;

    public sealed class MurkEntry
    {
        public Vector2 Position;
        public float Intensity;
    }
}
