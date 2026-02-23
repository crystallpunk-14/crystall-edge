using Content.Shared.Damage;

namespace Content.Shared._White.Weapons.Melee.Components;

[RegisterComponent]
public sealed partial class WhiteMeleeSelfDamageComponent : Component
{
    [DataField]
    public DamageSpecifier DamageToSelf = new()
    {
        DamageDict = new()
        {
            { "Blunt", 1 },
        }
    };
}
