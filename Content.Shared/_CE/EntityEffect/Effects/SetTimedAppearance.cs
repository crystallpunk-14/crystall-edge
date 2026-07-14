using Content.Shared._CE.TimedAppearance;

namespace Content.Shared._CE.EntityEffect.Effects;

/// <summary>
/// Activates a named visual state on the target entity for a fixed duration using
/// <see cref="CETimedAppearanceSystem"/> and <see cref="Robust.Shared.GameObjects.AppearanceComponent"/>
/// as the networking layer.
/// <para/>
/// The entity must have <see cref="Robust.Shared.GameObjects.AppearanceComponent"/> and
/// <see cref="Robust.Client.GameObjects.GenericVisualizerComponent"/> for the visual change
/// to be visible on clients.
/// </summary>
/// <example>
/// <code>
/// - !type:SetTimedAppearance
///   key: attack_open
///   duration: 1.2
/// </code>
/// </example>
public sealed partial class SetTimedAppearance : CEEntityEffectBase<SetTimedAppearance>
{
    public SetTimedAppearance()
    {
        EffectTarget = CEEffectTarget.User;
    }

    /// <summary>
    /// The named visual state to activate.
    /// This must match a key used by the entity's <c>GenericVisualizer</c> mapping for
    /// <see cref="CEAnCEAnimationAppearanceVisuals.Key
    /// </summary>
    [DataField(required: true)]
    public string Key = string.Empty;

    /// <summary>
    /// How long the visual state stays active, in seconds.
    /// After this time the appearance is automatically restored to its previous state.
    /// </summary>
    [DataField(required: true)]
    public float Duration;
}

public sealed partial class CESetTimedAppearanceEffectSystem : CEEntityEffectSystem<SetTimedAppearance>
{
    [Dependency] private CETimedAppearanceSystem _timedAppearance = default!;

    protected override void Effect(ref CEEntityEffectEvent<SetTimedAppearance> args)
    {
        if (ResolveEffectEntity(args.Args, args.Effect.EffectTarget) is not { } entity)
            return;

        _timedAppearance.SetTimedAppearance(entity, args.Effect.Key, args.Effect.Duration);
    }
}
