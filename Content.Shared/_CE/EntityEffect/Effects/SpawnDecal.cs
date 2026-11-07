using System.Numerics;
using Content.Shared.Decals;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._CE.EntityEffect.Effects;

public enum CEDecalRotation : byte
{
    /// <summary>
    /// Only <see cref="SpawnDecal.RotationOffset"/>.
    /// </summary>
    Fixed,

    /// <summary>
    /// Any angle.
    /// </summary>
    Random,

    /// <summary>
    /// The decal's bottom faces the direction it was thrown in, so south-facing art points away from the center.
    /// </summary>
    Direction,
}

/// <summary>
/// Places decals around the effect point. Every decal is thrown along the effect angle
/// (<see cref="CEEntityEffectArgs.Angle"/>, see <see cref="Radial"/>) at a random distance.
/// </summary>
public sealed partial class SpawnDecal : CEEntityEffectBase<SpawnDecal>
{
    /// <summary>
    /// Each decal is picked at random from this list.
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<DecalPrototype>> Decals = new();

    [DataField]
    public int Count = 1;

    [DataField]
    public float MinDistance;

    [DataField]
    public float MaxDistance;

    /// <summary>
    /// Random deviation of every decal from the effect angle, up to this angle either way.
    /// </summary>
    [DataField]
    public Angle Spread = Angle.Zero;

    /// <summary>
    /// Shift of the center, turned by the effect angle.
    /// </summary>
    [DataField]
    public Vector2 Offset = Vector2.Zero;

    [DataField]
    public CEDecalRotation Rotation = CEDecalRotation.Random;

    [DataField]
    public Angle RotationOffset = Angle.Zero;

    [DataField]
    public Color? Color;

    [DataField]
    public bool Cleanable = true;

    [DataField]
    public int ZIndex;
}

public sealed partial class CESpawnDecalEffectSystem : CEEntityEffectSystem<SpawnDecal>
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedDecalSystem _decals = default!;

    /// <summary>
    /// Decals are positioned by their bottom left corner.
    /// </summary>
    private static readonly Vector2 HalfTile = new(0.5f, 0.5f);

    protected override void Effect(ref CEEntityEffectEvent<SpawnDecal> args)
    {
        // Decals are server-driven; the client gets them through the decal chunk state.
        if (_net.IsClient)
            return;

        var effect = args.Effect;
        if (effect.Decals.Count == 0 || !TryResolveEffectCoordinates(args.Args, effect.EffectTarget, out var coords))
            return;

        if (TransformSystem.GetGrid(coords) is not { } grid)
            return;

        var center = TransformSystem.ToMapCoordinates(coords);
        center = center.Offset(args.Args.Angle.RotateVec(effect.Offset));
        var gridRotation = TransformSystem.GetWorldRotation(grid);
        var spread = (float) effect.Spread.Theta;

        for (var i = 0; i < effect.Count; i++)
        {
            var direction = args.Args.Angle;
            if (spread > 0f)
                direction += _random.NextFloat(-spread, spread);

            var distance = effect.MaxDistance > effect.MinDistance
                ? _random.NextFloat(effect.MinDistance, effect.MaxDistance)
                : effect.MinDistance;

            var position = center.Offset(direction.ToWorldVec() * distance);
            var local = TransformSystem.ToCoordinates(grid, position);

            var rotation = effect.Rotation switch
            {
                CEDecalRotation.Random => _random.NextAngle(),
                CEDecalRotation.Direction => direction - gridRotation,
                _ => Angle.Zero,
            } + effect.RotationOffset;

            var decal = new Decal(local.Position - HalfTile, _random.Pick(effect.Decals).Id, effect.Color, rotation, effect.ZIndex, effect.Cleanable);
            _decals.TryAddDecal(decal, local, out _);
        }
    }
}
