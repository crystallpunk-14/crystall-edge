using Content.Shared._CE.Character;
using Content.Shared._CE.Ghost;
using Content.Shared._CE.Objectives;
using Content.Shared._CE.Objectives.Components;
using Content.Shared.Ghost.Components;
using Content.Shared.Mind;

namespace Content.Server._CE.Character;

/// <summary>
/// Answers a ghost's <see cref="CEViewCharacterRequestEvent"/> with another player's objectives,
/// resolved server-side since objective data hangs off the target's mind (never PVS-networked to
/// anyone but its owner). Skills aren't handled here - CESkillStorageComponent has no owner
/// restriction, so the client reads those straight off the target entity (see CEClientSkillSystem).
/// </summary>
public sealed partial class CEViewCharacterSystem : EntitySystem
{
    [Dependency] private SharedMindSystem _minds = default!;
    [Dependency] private CESharedObjectiveSystem _objectives = default!;
    [Dependency] private CEGhostMetaSystem _ghostMeta = default!;
    [Dependency] private CECharacterSystem _character = default!;

    [Dependency] private EntityQuery<GhostComponent> _ghostQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<CEViewCharacterRequestEvent>(OnRequest);
    }

    private void OnRequest(CEViewCharacterRequestEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { Valid: true } sender || !_ghostQuery.HasComp(sender))
        {
            Log.Warning($"User {args.SenderSession.Name} sent a {nameof(CEViewCharacterRequestEvent)} without being a ghost.");
            return;
        }

        if (!_ghostMeta.CanSeeMeta(sender))
            return;

        var target = GetEntity(msg.Target);
        var objectives = new List<CEObjectiveInfo>();

        if (Exists(target) && TryGetTargetMind(target, out var mindId))
        {
            foreach (var objective in _objectives.GetObjectives(mindId))
            {
                objectives.Add(BuildObjectiveInfo(objective));
            }
        }

        RaiseNetworkEvent(new CEViewCharacterResponseEvent(msg.Target, objectives), args.SenderSession.Channel);
    }

    // A character's body leads to its mind even after the player left it, e.g. a corpse.
    private bool TryGetTargetMind(EntityUid target, out EntityUid mindId)
    {
        if (_character.TryGetCharacterByBody(target, out var character))
        {
            mindId = character.Value.Owner;
            return true;
        }

        return _minds.TryGetMind(target, out mindId, out _);
    }

    private CEObjectiveInfo BuildObjectiveInfo(Entity<CEObjectiveComponent> objective)
    {
        var descriptor = CompOrNull<CEObjectiveDescriptorComponent>(objective);

        return new CEObjectiveInfo
        {
            Title = Name(objective),
            Description = MetaData(objective).EntityDescription,
            Progress = _objectives.GetProgress(objective.AsNullable()),
            PrototypeId = Prototype(objective)?.ID,
            DescriptorName = descriptor != null ? Loc.GetString(descriptor.Name) : null,
            DescriptorColor = descriptor?.Color,
            DescriptorTooltip = descriptor?.Tooltip != null ? Loc.GetString(descriptor.Tooltip.Value) : null,
        };
    }
}
