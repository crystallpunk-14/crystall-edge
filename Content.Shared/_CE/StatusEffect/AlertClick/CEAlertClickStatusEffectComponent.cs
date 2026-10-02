using Content.Shared._CE.EntityEffect;
using Content.Shared.Alert;
using Robust.Shared.GameStates;

namespace Content.Shared._CE.StatusEffect.AlertClick;

/// <summary>
/// Runs <see cref="Effects"/> when the owner of the status effect clicks the alert shown by the same
/// status effect's <c>StatusEffectAlert</c>. Source and target of the effects are the clicking entity.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CEAlertClickStatusEffectComponent : Component
{
    [DataField(required: true)]
    public List<CEEntityEffect> Effects = new();
}

/// <summary>
/// Alert click event handled by <see cref="CEAlertClickStatusEffectComponent"/>, use it as <c>clickEvent</c>
/// of the alert.
/// </summary>
public sealed partial class CEStatusEffectAlertClickEvent : BaseAlertEvent;
