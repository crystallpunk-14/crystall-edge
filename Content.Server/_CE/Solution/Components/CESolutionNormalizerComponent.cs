using Content.Server._CE.Solution.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;

namespace Content.Server._CE.Solution.Components;

/// <summary>
/// Gradually destroys the least-abundant reagent in a solution, one tick at a time, "normalizing" a
/// poured-in mixture down toward its dominant reagents. Requires external (APC) power to run.
/// </summary>
[RegisterComponent, Access(typeof(CESolutionNormalizerSystem)), AutoGenerateComponentPause]
public sealed partial class CESolutionNormalizerComponent : Component
{
    [DataField(required: true)]
    public string SolutionName = string.Empty;

    [ViewVariables]
    public Entity<SolutionComponent>? Solution;

    /// <summary>
    /// How much of the least-abundant reagent is destroyed per tick.
    /// </summary>
    [DataField]
    public FixedPoint2 LeakageQuantity = FixedPoint2.New(1f);

    [DataField]
    public TimeSpan UpdateFrequency = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Next time <see cref="UpdateFrequency"/> allows a tick.
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan NextUpdateTime = TimeSpan.Zero;

    [DataField]
    public SoundSpecifier NormalizeSound = new SoundPathSpecifier("/Audio/Ambience/Objects/drain.ogg")
    {
        Params = AudioParams.Default.WithVariation(0.03f),
    };
}