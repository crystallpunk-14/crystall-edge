using System.Numerics;
using Content.Shared._CE.Alchemy.Prototypes;
using Content.Shared._CE.Alchemy.World;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Alchemy;

/// <summary>
/// Working state of a recipe replay. Not stored anywhere: it is rebuilt from the recipe on every analysis.
/// </summary>
public struct CEAlchemyState
{
    /// <summary>
    /// Current marker position on the brewing map (already an average, not a sum).
    /// </summary>
    public Vector2 Pos;

    /// <summary>
    /// Units of carriers poured so far.
    /// </summary>
    public float Carriers;

    /// <summary>
    /// Whole volume poured so far, inert reagents and water included.
    /// </summary>
    public float Volume;

    /// <summary>
    /// Sum of amount * stock over all carriers.
    /// </summary>
    public float Stock;

    /// <summary>
    /// Length of the marker path inside every zone.
    /// </summary>
    public Dictionary<ProtoId<CEAlchemyZonePrototype>, float> ZoneLengths;
}

/// <summary>
/// What an alchemy effect knows about the step it is applied for.
/// </summary>
public readonly record struct CEAlchemyApplyArgs(ProtoId<ReagentPrototype> Reagent, float Amount, CEAlchemySystem System);
