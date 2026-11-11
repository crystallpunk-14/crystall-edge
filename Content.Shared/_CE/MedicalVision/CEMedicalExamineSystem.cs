using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._CE.MedicalVision;

/// <summary>
/// With medical vision the health examine shows exact numbers grouped like the health analyzer:
/// condition, damage by group and type, stamina, blood and bleeding. Never diseases or reagents.
/// </summary>
public sealed partial class CEMedicalExamineSystem : EntitySystem
{
    private static readonly SpriteSpecifier StaminaIcon =
        new SpriteSpecifier.Rsi(new ResPath("_CE/Interface/Medical/examine_icons.rsi"), "stamina");

    private static readonly SpriteSpecifier BloodIcon =
        new SpriteSpecifier.Rsi(new ResPath("_CE/Interface/Medical/examine_icons.rsi"), "blood");

    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private MobThresholdSystem _thresholds = default!;
    [Dependency] private SharedStaminaSystem _stamina = default!;
    [Dependency] private BloodstreamSystem _bloodstream = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    /// <summary>
    /// Medical vision from worn clothing (not pockets) or from a status effect
    /// </summary>
    public bool HasMedicalVision(EntityUid uid)
    {
        if (_statusEffects.HasEffectComp<CEMedicalVisionComponent>(uid))
            return true;

        if (!_inventory.TryGetContainerSlotEnumerator(uid, out var slots, ~SlotFlags.POCKET))
            return false;

        while (slots.NextItem(out var item))
        {
            if (HasComp<CEMedicalVisionComponent>(item))
                return true;
        }

        return false;
    }

    public bool TryCreateExactMarkup(EntityUid examiner, Entity<DamageableComponent> target, out FormattedMessage message)
    {
        message = new FormattedMessage();

        if (!HasMedicalVision(examiner))
            return false;

        AddCondition(message, target);
        AddDamage(message, target);
        AddStamina(message, target);
        AddBlood(message, target);

        return true;
    }

    private void AddCondition(FormattedMessage message, EntityUid target)
    {
        if (!TryComp<MobStateComponent>(target, out var mobState))
            return;

        var (state, color) = _mobState.IsDead(target, mobState)
            ? ("ce-medical-examine-state-dead", Color.Red)
            : _mobState.IsCritical(target, mobState)
                ? ("ce-medical-examine-state-critical", Color.Yellow)
                : ("ce-medical-examine-state-alive", Color.LimeGreen);

        message.AddMarkupOrThrow(Loc.GetString("ce-medical-examine-state",
            ("state", Loc.GetString(state)),
            ("color", color.ToHex())));
    }

    private void AddDamage(FormattedMessage message, Entity<DamageableComponent> target)
    {
        var total = Round(_damageable.GetTotalDamage(target.AsNullable()));

        message.PushNewline();
        if (_thresholds.TryGetThresholdForState(target, MobState.Critical, out var crit)
            && _thresholds.TryGetThresholdForState(target, MobState.Dead, out var dead))
        {
            message.AddMarkupOrThrow(Loc.GetString("ce-medical-examine-total-thresholds",
                ("damage", total),
                ("crit", Round(crit.Value)),
                ("dead", Round(dead.Value))));
        }
        else
        {
            message.AddMarkupOrThrow(Loc.GetString("ce-medical-examine-total", ("damage", total)));
        }

        var damage = _damageable.GetAllDamage(target.AsNullable());
        foreach (var (groupId, groupAmount) in _damageable.GetDamagePerGroup(target.AsNullable()))
        {
            if (Round(groupAmount) <= 0 || !_proto.Resolve<DamageGroupPrototype>(groupId, out var group))
                continue;

            message.PushNewline();
            message.AddMarkupOrThrow(Icon(group.Icon) + Loc.GetString("ce-medical-examine-group",
                ("group", group.LocalizedName),
                ("damage", Round(groupAmount))));

            foreach (var typeId in group.DamageTypes)
            {
                if (!damage.DamageDict.TryGetValue(typeId, out var typeAmount)
                    || Round(typeAmount) <= 0
                    || !_proto.Resolve(typeId, out var type))
                    continue;

                message.PushNewline();
                message.AddMarkupOrThrow(Loc.GetString("ce-medical-examine-type",
                    ("type", type.LocalizedName),
                    ("damage", Round(typeAmount))));
            }
        }
    }

    private void AddStamina(FormattedMessage message, EntityUid target)
    {
        if (!TryComp<StaminaComponent>(target, out var stamina) || stamina.CritThreshold <= 0f)
            return;

        var current = MathF.Max(0f, stamina.CritThreshold - _stamina.GetStaminaDamage(target, stamina));

        message.PushNewline();
        message.AddMarkupOrThrow(Icon(StaminaIcon) + Loc.GetString("ce-medical-examine-stamina",
            ("current", (int) MathF.Round(current)),
            ("max", (int) MathF.Round(stamina.CritThreshold))));
    }

    private void AddBlood(FormattedMessage message, EntityUid target)
    {
        if (!TryComp<BloodstreamComponent>(target, out var blood))
            return;

        var level = _bloodstream.GetBloodLevel((target, blood));
        var color = level < blood.BloodlossThreshold ? Color.Red : Color.White;

        message.PushNewline();
        message.AddMarkupOrThrow(Icon(BloodIcon) + Loc.GetString("ce-medical-examine-blood",
            ("percent", (int) MathF.Round(level * 100f)),
            ("color", color.ToHex())));

        var bleeding = (int) MathF.Round(blood.BleedAmount);
        message.PushNewline();
        message.AddMarkupOrThrow(bleeding > 0
            ? Loc.GetString("ce-medical-examine-bleeding", ("amount", bleeding))
            : Loc.GetString("ce-medical-examine-no-bleeding"));
    }

    private static int Round(FixedPoint2 value)
    {
        return (int) MathF.Round(value.Float());
    }

    private static string Icon(SpriteSpecifier? icon)
    {
        return icon switch
        {
            SpriteSpecifier.Rsi rsi => $"[ceicon rsi=\"{rsi.RsiPath}\" state=\"{rsi.RsiState}\"/] ",
            SpriteSpecifier.Texture texture => $"[ceicon texture=\"{texture.TexturePath}\"/] ",
            _ => string.Empty,
        };
    }
}
