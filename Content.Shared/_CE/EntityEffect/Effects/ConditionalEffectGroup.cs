namespace Content.Shared._CE.EntityEffect.Effects;

/// <summary>
/// Applies inner effects only when conditions pass.
/// With <see cref="RequireAll"/> true (default), ALL conditions must pass.
/// With <see cref="RequireAll"/> false, ANY one condition suffices.
/// An empty <see cref="Conditions"/> list always passes.
/// </summary>
public sealed partial class ConditionalEffectGroup : CEEntityEffectBase<ConditionalEffectGroup>
{
    [DataField]
    public List<CEEntityCondition> Conditions = new();

    [DataField]
    public List<CEEntityEffect> Effects = new();

    /// <summary>
    /// When true (default), all conditions must pass.
    /// When false, at least one condition must pass.
    /// </summary>
    [DataField]
    public bool RequireAll = true;
}

public sealed partial class CEConditionalEffectGroupSystem : CEEntityEffectSystem<ConditionalEffectGroup>
{
    protected override void Effect(ref CEEntityEffectEvent<ConditionalEffectGroup> args)
    {
        if (args.Effect.Conditions.Count > 0)
        {
            if (args.Effect.RequireAll)
            {
                foreach (var condition in args.Effect.Conditions)
                {
                    if (!condition.Passes(args.Args))
                        return;
                }
            }
            else
            {
                var anyPassed = false;
                foreach (var condition in args.Effect.Conditions)
                {
                    if (condition.Passes(args.Args))
                    {
                        anyPassed = true;
                        break;
                    }
                }

                if (!anyPassed)
                    return;
            }
        }

        foreach (var effect in args.Effect.Effects)
        {
            effect.Effect(args.Args);
        }
    }
}
