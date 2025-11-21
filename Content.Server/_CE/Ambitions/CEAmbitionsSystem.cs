using System.Diagnostics.CodeAnalysis;
using Content.Server.Mind;
using Content.Shared._CE.Ambitions;
using Content.Shared._CE.Ambitions.Components;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Objectives.Components;
using Content.Shared.Objectives.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._CE.Ambitions;

public sealed class CEAmbitionsSystem : CESharedAmbitionsSystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly SharedObjectivesSystem _objectives = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;

    private readonly HashSet<EntityPrototype> _ambitions = new();

    public override void Initialize()
    {
        base.Initialize();
        CacheAmbitions();

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawningComplete);
        SubscribeLocalEvent<CEAmbitionsSetupComponent, MapInitEvent>(OnMindAdded);
        SubscribeLocalEvent<CEAmbitionObjectiveComponent, ObjectiveAfterAssignEvent>(OnObjectiveAssigned);
        SubscribeLocalEvent<CEAmbitionObjectiveComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnPlayerSpawningComplete(PlayerSpawnCompleteEvent ev)
    {
        EnsureComp<CEAmbitionsSetupComponent>(ev.Mob);
    }

    private void OnGetProgress(Entity<CEAmbitionObjectiveComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = 1f;
    }

    private void OnObjectiveAssigned(Entity<CEAmbitionObjectiveComponent> ent, ref ObjectiveAfterAssignEvent args)
    {
        var title = Loc.GetString(ent.Comp.Name);
        var desc = Loc.GetString(ent.Comp.Desc);

        foreach (var (key, parseEntry) in ent.Comp.Parsings)
        {
            var parseKey = $"!{key}!";
            var parseValue = parseEntry.GetText(EntityManager, _proto, _random);

            title = title.Replace(parseKey, parseValue);
            desc = desc.Replace(parseKey, parseValue);
        }

        _meta.SetEntityDescription(ent, desc);
        _meta.SetEntityName(ent, title);
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs ev)
    {
        if (!ev.WasModified<EntityPrototype>())
            return;

        CacheAmbitions();
    }

    private void CacheAmbitions()
    {
        _ambitions.Clear();
        foreach (var objective in _proto.EnumeratePrototypes<EntityPrototype>())
        {
            if (!objective.Components.TryGetComponent<ObjectiveComponent>(_compFactory, out var objectiveComponent))
                continue;
            if (!objective.Components.TryGetComponent<CEAmbitionObjectiveComponent>(_compFactory, out var ambition))
                continue;

            _ambitions.Add(objective);
        }
    }

    private void OnMindAdded(Entity<CEAmbitionsSetupComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.EndTime = _timing.CurTime + ent.Comp.AvailableTime;
        var guardCounter = 20;

        var createdAmbitions = 0;
        while (createdAmbitions < ent.Comp.MaxAmbitions)
        {
            if (TryAddAmbition(ent))
                createdAmbitions++;
            else
                guardCounter--;

            if (guardCounter == 0)
            {
                Log.Error("Ambitions were not generated after 20 tries for entity");
                break;
            }
        }
    }

    private bool CheckSuitableAmbition(Entity<CEAmbitionsSetupComponent> ent, [NotNullWhen(true)] EntityPrototype? objective)
    {
        if (objective == null)
            return false;

        var suitableAmbition = true;

        if (!_mind.TryGetMind(ent, out var mind, out var mindComp))
            return false;

        foreach (var obj in mindComp.Objectives)
        {
            if (MetaData(obj).EntityPrototype == objective)
                suitableAmbition = false;
        }

        return suitableAmbition;
    }

    private bool TryAddAmbition(Entity<CEAmbitionsSetupComponent> ent)
    {
        var newAmbition = GenerateAmbition();

        if (!CheckSuitableAmbition(ent, newAmbition))
            return false;

        if (!_mind.TryGetMind(ent.Owner, out var mind, out var mindId))
            return false;

        if (!_mind.TryAddObjective(mind, mindId, newAmbition.ID))
            return false;

        return true;
    }

    //private bool TryRerollAmbition(Entity<CEAmbitionsSetupComponent> ent, int index)
    //{
    //    if (ent.Comp.Ambitions.Count < index + 1)
    //        return false;
    //    var guardCounter = 20;
//
    //    ObjectiveInfo? newAmbition = null;
    //    var generated = false;
    //    while (generated != true)
    //    {
    //        newAmbition = GenerateAmbition();
    //        if (CheckSuitableAmbition(ent, newAmbition))
    //            generated = true;
//
    //        guardCounter--;
    //        if (guardCounter == 0)
    //        {
    //            Log.Error("Ambitions were not rerolled after 20 tries");
    //            break;
    //        }
    //    }
//
    //    if (!generated || newAmbition == null)
    //        return false;
//
    //    ent.Comp.Ambitions[index] = newAmbition.Value;
    //    return true;
    //}

    public EntityPrototype? GenerateAmbition()
    {
        if (_ambitions.Count == 0)
        {
            Log.Error("No ambitions found");
            return null;
        }

        return _random.Pick(_ambitions);
    }
}
