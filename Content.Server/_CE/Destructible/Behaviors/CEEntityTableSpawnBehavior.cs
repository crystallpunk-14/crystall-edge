using System.Numerics;
using Content.Server.Destructible;
using Content.Server.Destructible.Thresholds.Behaviors;
using Content.Shared.EntityTable;
using Content.Shared.EntityTable.EntitySelectors;
using Robust.Server.GameObjects;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Server._CE.Destructible.Behaviors;

/// <summary>
/// Spawns the result of an entity table around the destroyed entity and sends the spawns flying in random directions.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class CEEntityTableSpawnBehavior : IThresholdBehavior
{
    [DataField(required: true)]
    public EntityTableSelector Table = default!;

    /// <summary>
    /// Max distance from the destroyed entity the spawns are scattered to, per axis.
    /// </summary>
    [DataField]
    public float Offset = 0.3f;

    /// <summary>
    /// Range of the random speed the spawns are launched with.
    /// </summary>
    [DataField]
    public float MinSpeed = 0.5f;

    [DataField]
    public float MaxSpeed = 2f;

    public void Execute(EntityUid owner, DestructibleSystem system, EntityUid? cause = null)
    {
        var entMan = system.EntityManager;
        var transform = entMan.System<TransformSystem>();
        var physics = entMan.System<SharedPhysicsSystem>();
        var position = transform.GetMapCoordinates(owner);

        foreach (var proto in entMan.System<EntityTableSystem>().GetSpawns(Table, system.Random))
        {
            var scatter = new Vector2(system.Random.NextFloat(-Offset, Offset), system.Random.NextFloat(-Offset, Offset));
            var spawned = entMan.SpawnEntity(proto, position.Offset(scatter));

            transform.SetLocalRotation(spawned, system.Random.NextAngle());

            if (entMan.TryGetComponent<PhysicsComponent>(spawned, out var body))
                physics.SetLinearVelocity(spawned, system.Random.NextAngle().ToVec() * system.Random.NextFloat(MinSpeed, MaxSpeed), body: body);
        }
    }
}
