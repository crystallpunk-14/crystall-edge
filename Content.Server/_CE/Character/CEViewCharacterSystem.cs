using Content.Shared._CE.Character;
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

        var target = GetEntity(msg.Target);
        var objectives = new List<CEObjectiveInfo>();

        if (Exists(target) && _minds.TryGetMind(target, out var mindId, out _))
        {
            foreach (var objective in _objectives.GetObjectives(mindId))
            {
                objectives.Add(BuildObjectiveInfo(objective));
            }
        }

        RaiseNetworkEvent(new CEViewCharacterResponseEvent(msg.Target, objectives), args.SenderSession.Channel);
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
