using Content.Shared._CE.EntityEffect;
using Content.Shared._CE.Science.Components;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._CE.Science;

public abstract partial class CESharedScienceSystem
{
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    private void InitializeAchievement()
    {
        SubscribeLocalEvent<CEScienceAchievementHolderComponent, MapInitEvent>(OnAchievementMapInit);
        SubscribeLocalEvent<CEScienceAchievementHolderComponent, UseInHandEvent>(OnAchievementUseInHand);
        SubscribeLocalEvent<CEScienceAchievementHolderComponent, GetVerbsEvent<AlternativeVerb>>(AddAchievementVerbs);
        SubscribeLocalEvent<CEScienceAchievementHolderComponent, CEScienceAchievementDoAfterEvent>(OnAchievementDoAfter);
    }

    private void OnAchievementMapInit(Entity<CEScienceAchievementHolderComponent> ent, ref MapInitEvent args)
    {
        if (!_proto.TryIndex(ent.Comp.Achievement, out var achievement))
            return;

        _metaData.SetEntityName(ent, Loc.GetString(achievement.Name));

        if (achievement.Desc is { } desc)
            _metaData.SetEntityDescription(ent, Loc.GetString(desc));
    }

    private void OnAchievementUseInHand(Entity<CEScienceAchievementHolderComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = StartAchievementRead(ent, args.User);
    }

    private void AddAchievementVerbs(Entity<CEScienceAchievementHolderComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("ce-science-achievement-verb-study"),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/examine.svg.192dpi.png")),
            Act = () => StartAchievementRead(ent, user),
            Priority = 2,
        });
    }

    private bool StartAchievementRead(Entity<CEScienceAchievementHolderComponent> ent, EntityUid user)
    {
        if (!_proto.TryIndex(ent.Comp.Achievement, out var achievement))
            return false;

        var doAfterArgs = new DoAfterArgs(EntityManager, user, achievement.Time, new CEScienceAchievementDoAfterEvent(), ent, target: user, used: ent)
        {
            BreakOnMove = false,
            BreakOnDamage = true,
            NeedHand = _hands.IsHolding(user, ent),
        };

        return _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnAchievementDoAfter(Entity<CEScienceAchievementHolderComponent> ent, ref CEScienceAchievementDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target)
            return;

        if (!_proto.TryIndex(ent.Comp.Achievement, out var achievement))
            return;

        foreach (var effect in achievement.Effects)
        {
            var effectArgs = new CEEntityEffectArgs(
                EntityManager,
                Source: args.User,
                Used: ent,
                Angle: Angle.Zero,
                Speed: 0f,
                Target: target,
                Position: null);

            effect.Effect(effectArgs);
        }

        args.Handled = true;
    }
}

[Serializable, NetSerializable]
public sealed partial class CEScienceAchievementDoAfterEvent : SimpleDoAfterEvent;
