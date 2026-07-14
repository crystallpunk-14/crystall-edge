using System.Numerics;
using Content.Shared._CE.Animation.Floating;
using Robust.Client.GameObjects;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Client._CE.Animation.Floating;

/// <summary>
/// Client-side system that drives the floating animation defined by
/// <see cref="Shared._CE.Animation.Floating.CEAutoFloatingVisualsComponent"/>.
/// Each entity gets a random phase offset on startup so groups of entities bob out of sync.
/// </summary>
public sealed partial class CEAutoFloatingVisualsSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEAutoFloatingVisualsComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<CEAutoFloatingVisualsComponent> ent, ref ComponentStartup args)
    {
        // Pick a random phase within the full cycle so co-spawned entities desynchronize.
        var cycle = ent.Comp.AnimationTime * 2f;
        ent.Comp.PhaseOffset = _random.NextFloat() * cycle;
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        // Use real time so the wave continues smoothly regardless of pause/resume of game time.
        var now = (float) _timing.RealTime.TotalSeconds;

        var query = EntityQueryEnumerator<CEAutoFloatingVisualsComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var comp, out var sprite))
        {
            var period = comp.AnimationTime * 2f;
            if (period <= 0f)
                continue;

            // Phase progresses 0 -> 2π over a full cycle.
            var t = (now + comp.PhaseOffset) % period / period;
            // Sine maps to [-1, 1]; remap to [0, 1] so 0 = StartOffset, 1 = FloatingOffset.
            var k = (MathF.Sin(t * MathF.Tau) + 1f) * 0.5f;

            var offset = Vector2.Lerp(comp.FloatingStartOffset, comp.FloatingOffset, k);
            if (sprite.Offset != offset)
                _sprite.SetOffset(uid, offset);
        }
    }
}
