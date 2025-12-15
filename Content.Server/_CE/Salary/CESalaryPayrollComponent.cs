using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Salary;

/// <summary>
/// Pays out the salary upon interaction, if it has accumulated for the player.
/// </summary>
[RegisterComponent, Access(typeof(CESalarySystem))]
public sealed partial class CESalaryPayrollComponent : Component
{
    [DataField]
    public SoundSpecifier BuySound = new SoundPathSpecifier("/Audio/_CE/Effects/cash.ogg")
    {
        Params = AudioParams.Default.WithVariation(0.1f),
    };

    [DataField]
    public EntProtoId BuyVisual = "CECashImpact";
}
