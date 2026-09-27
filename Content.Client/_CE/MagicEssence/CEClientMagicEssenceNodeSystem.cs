using Content.Client._CE.TimedDespawnFadeout;
using Content.Shared._CE.MagicEssence.Components;
using Content.Shared._CE.MagicEssence.Prototypes;
using Content.Shared._CE.TimedDespawn;
using Robust.Client.GameObjects;
using Robust.Shared.Analyzers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._CE.MagicEssence;

public sealed partial class CEClientMagicEssenceNodeSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedPointLightSystem _pointLight = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    [SubscribeLocalEvent]
    private void OnAfterHandleState(Entity<CEMagicEssenceNodeComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateVisuals(ent);
    }

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<CEMagicEssenceNodeComponent> ent, ref MapInitEvent args)
    {
        UpdateVisuals(ent);
    }

    /// <summary>
    /// Updates the essence-colored sprite layers and re-caches <see cref="CEMagicEssenceNodeComponent.LightColor"/>
    /// (the 70/20/10 essence blend used by <see cref="FrameUpdate"/>) whenever the rolled aspects change.
    /// </summary>
    private void UpdateVisuals(Entity<CEMagicEssenceNodeComponent> ent)
    {
        ent.Comp.LightColor = GetLightColor(ent.Comp);

        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        SetLayerColor((ent, sprite), ent.Comp.EssenceALayer, ent.Comp.EssenceA);
        SetLayerColor((ent, sprite), ent.Comp.EssenceBLayer, ent.Comp.EssenceB);
        SetLayerColor((ent, sprite), ent.Comp.EssenceCLayer, ent.Comp.EssenceC);
    }

    private void SetLayerColor(Entity<SpriteComponent?> sprite, string layer, ProtoId<CEMagicEssenceTypePrototype>? essenceId)
    {
        if (essenceId is not { } id || !_proto.TryIndex(id, out var essence))
            return;

        _sprite.LayerSetColor(sprite, layer, essence.Color);
    }

    /// <summary>
    /// Drives the point light's energy off the same fade curve <see cref="CETimedDespawnFadeoutSystem"/>
    /// applies to the node's sprite alpha (the node is expected to have a paired
    /// <see cref="CETimedDespawnFadeoutComponent"/> and <see cref="CERandomizedTimedDespawnComponent"/>).
    /// The light's color is a 70/20/10 blend of the node's 3 rolled essence aspects (matching the
    /// essence generation weights).
    /// </summary>
    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var query = EntityQueryEnumerator<CEMagicEssenceNodeComponent, CERandomizedTimedDespawnComponent, CETimedDespawnFadeoutComponent, PointLightComponent>();
        while (query.MoveNext(out var uid, out var node, out var randomized, out var fadeout, out var light))
        {
            if (randomized.Lifetime <= TimeSpan.Zero)
                continue;

            var elapsed = Math.Clamp((float)((_timing.CurTime - randomized.SpawnTime) / randomized.Lifetime), 0f, 1f);
            var alpha = CETimedDespawnFadeoutSystem.GetFadeAlpha(elapsed, fadeout.FadeInEnd, fadeout.FadeOutStart);

            _pointLight.SetColor(uid, node.LightColor ?? Color.White, light);
            _pointLight.SetEnergy(uid, alpha, light);
        }
    }

    private Color GetLightColor(CEMagicEssenceNodeComponent node)
    {
        var r = 0f;
        var g = 0f;
        var b = 0f;
        var totalWeight = 0f;

        AccumulateEssenceColor(node.EssenceA, 0.7f, ref r, ref g, ref b, ref totalWeight);
        AccumulateEssenceColor(node.EssenceB, 0.2f, ref r, ref g, ref b, ref totalWeight);
        AccumulateEssenceColor(node.EssenceC, 0.1f, ref r, ref g, ref b, ref totalWeight);

        return totalWeight > 0f ? new Color(r / totalWeight, g / totalWeight, b / totalWeight) : Color.White;
    }

    private void AccumulateEssenceColor(ProtoId<CEMagicEssenceTypePrototype>? essenceId, float weight, ref float r, ref float g, ref float b, ref float totalWeight)
    {
        if (essenceId is not { } id || !_proto.TryIndex(id, out var essence))
            return;

        r += essence.Color.R * weight;
        g += essence.Color.G * weight;
        b += essence.Color.B * weight;
        totalWeight += weight;
    }
}
