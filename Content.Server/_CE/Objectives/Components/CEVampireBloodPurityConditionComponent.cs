using Content.Server._CE.Objectives.Systems;
using Robust.Shared.Utility;

namespace Content.Server._CE.Objectives.Components;

[RegisterComponent, Access(typeof(CEVampireObjectiveConditionsSystem))]
public sealed partial class CEVampireBloodPurityConditionComponent : Component
{
    [DataField]
    public SpriteSpecifier Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/_CE/Actions/vampire.rsi"), "blood_moon");
}
