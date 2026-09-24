using System.Numerics;

namespace Content.Client._CE.Murk;

// Scratch space for one map's projected debug circles, refilled every draw.
public sealed class CEMurkDebugBuffer
{
    public const int MaxCount = 64;

    public readonly Vector2[] FreePositions = new Vector2[MaxCount];
    public readonly float[] FreeRadii = new float[MaxCount];
    public readonly float[] FreeStrengths = new float[MaxCount];
    public int FreeCount;

    public readonly Vector2[] WallPositions = new Vector2[MaxCount];
    public readonly float[] WallRadii = new float[MaxCount];
    public readonly float[] WallStrengths = new float[MaxCount];
    public int WallCount;
}
