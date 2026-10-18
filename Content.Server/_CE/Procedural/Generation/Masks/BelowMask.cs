namespace Content.Server._CE.Procedural.Generation.Masks;

/// <summary>
/// Runs the nested <see cref="LevelOffsetMask.Masks"/> against the same tile one z-level down.
/// </summary>
public sealed partial class BelowMask : LevelOffsetMask
{
    protected override int Offset => -1;
}
