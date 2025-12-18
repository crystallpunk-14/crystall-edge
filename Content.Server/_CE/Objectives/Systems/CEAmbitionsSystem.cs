using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Mind;
using Content.Shared._CE.Ambitions;
using Content.Shared._CE.Ambitions.Components;
using Content.Shared.Objectives.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._CE.Objectives.Systems;

public sealed class CEAmbitionsSystem : CESharedAmbitionsSystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;

    private readonly List<(EntityPrototype prototype, float weight)> _ambitions = new();

    public override void Initialize()
    {
        base.Initialize();
        CacheAmbitions();

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        SubscribeNetworkEvent<CEToggleAmbitionsScreenEvent>(OnToggleAmbitions);

        SubscribeLocalEvent<CEAmbitionsSetupComponent, BoundUIOpenedEvent>(OnBoundUIOpened);

        SubscribeLocalEvent<CEAmbitionsSetupComponent, CEAmbitionCreateMessage>(OnAmbitionCreateRequest);
        SubscribeLocalEvent<CEAmbitionsSetupComponent, CEAmbitionDeleteMessage>(OnAmbitionDeleteRequest);
        SubscribeLocalEvent<CEAmbitionsSetupComponent, CEAmbitionLockMessage>(OnAmbitionLockRequest);

        SubscribeLocalEvent<CEAmbitionObjectiveComponent, ObjectiveAfterAssignEvent>(OnObjectiveAssigned);
        SubscribeLocalEvent<CEAmbitionObjectiveComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnAmbitionCreateRequest(Entity<CEAmbitionsSetupComponent> ent, ref CEAmbitionCreateMessage args)
    {
        if (ent.Comp.RerollAmount <= 0)
            return;
        if (!_mind.TryGetMind(ent.Owner, out var mind, out var mindId))
            return;

        var ambitionCount = 0;
        foreach (var objective in mindId.Objectives)
        {
            if (TerminatingOrDeleted(objective))
                continue;

            if (!HasComp<CEAmbitionObjectiveComponent>(objective))
                continue;

            ambitionCount++;
        }

        if (ambitionCount >= ent.Comp.MaxAmbitions)
            return;

        var added = false;
        var guard = 0;
        while (!added)
        {
            added = TryAddAmbition(ent);
            guard++;
            if (guard >= 20)
                break;
        }

        if (!added)
            return;

        ent.Comp.RerollAmount--;
        DirtyField(ent, ent.Comp, nameof(CEAmbitionsSetupComponent.RerollAmount));
        UpdateUiState(ent);
    }

    private void OnAmbitionDeleteRequest(Entity<CEAmbitionsSetupComponent> ent, ref CEAmbitionDeleteMessage args)
    {
        if (!_mind.TryGetMind(ent.Owner, out var mind, out var mindId))
            return;

        var ambitions = mindId.Objectives
            .Select((obj, idx) => (obj, idx))
            .Where(t => !TerminatingOrDeleted(t.obj) && HasComp<CEAmbitionObjectiveComponent>(t.obj))
            .ToList();

        if (args.Index < 0 || args.Index >= ambitions.Count)
            return;

        var (_, objIndexInMind) = ambitions[args.Index];
        _mind.TryRemoveObjective(mind, mindId, objIndexInMind);
        UpdateUiState(ent);
    }

    private void OnAmbitionLockRequest(Entity<CEAmbitionsSetupComponent> ent, ref CEAmbitionLockMessage args)
    {
        RemCompDeferred<CEAmbitionsSetupComponent>(ent);
    }

    private void OnGetProgress(Entity<CEAmbitionObjectiveComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = 0f;
    }

    private void OnObjectiveAssigned(Entity<CEAmbitionObjectiveComponent> ent, ref ObjectiveAfterAssignEvent args)
    {
        var title = Loc.GetString(ent.Comp.Name);
        var desc = Loc.GetString(ent.Comp.Desc);

        foreach (var (key, parseEntry) in ent.Comp.Parsings)
        {
            var parseKey = $"!{key}!";
            var parseValue = parseEntry.GetText(EntityManager, _proto, _random, args.Mind.OwnedEntity);

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
            if (!objective.Components.TryGetComponent<ObjectiveComponent>(_compFactory, out _))
                continue;
            if (!objective.Components.TryGetComponent<CEAmbitionObjectiveComponent>(_compFactory, out var ambition))
                continue;

            _ambitions.Add((objective, ambition.Weight));
        }
    }

    private void OnToggleAmbitions(CEToggleAmbitionsScreenEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not {Valid: true} ent)
            return;

        if (!TryComp<CEAmbitionsSetupComponent>(ent, out var ambitions))
            return;

        if (!TryComp<ActorComponent>(ent, out var actor))
            return;

        _userInterface.TryToggleUi(ent, CEAmbitionsUIKey.Key, actor.PlayerSession);
    }

    private void OnBoundUIOpened(Entity<CEAmbitionsSetupComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUiState(ent);
    }

    private void UpdateUiState(Entity<CEAmbitionsSetupComponent> ent)
    {
        if (!_mind.TryGetMind(ent.Owner, out var mind, out var mindId))
            return;

        List<(string, string)> objectiveList = new();

        foreach (var objective in mindId.Objectives)
        {
            if (!HasComp<CEAmbitionObjectiveComponent>(objective))
                continue;

            var meta = MetaData(objective);
            objectiveList.Add((meta.EntityName, meta.EntityDescription));
        }

        var state = new CEAmbitionsBuiState(objectiveList, ent.Comp.RerollAmount, ent.Comp.MaxAmbitions);
        _userInterface.SetUiState(ent.Owner, CEAmbitionsUIKey.Key, state);
    }

    private bool CheckSuitableAmbition(Entity<CEAmbitionsSetupComponent> ent, [NotNullWhen(true)] EntityPrototype? objective)
    {
        if (objective == null)
            return false;

        var suitableAmbition = true;

        if (!objective.Components.TryGetComponent<CEAmbitionObjectiveComponent>(_compFactory, out var ambObj))
            return false;

        if (!_mind.TryGetMind(ent, out _, out var mindComp))
            return false;

        foreach (var obj in mindComp.Objectives)
        {
            if (MetaData(obj).EntityPrototype?.ID == objective.ID)
            {
                suitableAmbition = false;
                break;
            }
        }

        foreach (var condition in ambObj.Conditions)
        {
            if (!condition.Check(EntityManager, _proto, ent.Owner))
            {
                suitableAmbition = false;
                break;
            }
        }

        foreach (var (_, parsing) in ambObj.Parsings)
        {
            //The text can only be null if everything goes wrong—for example,
            //if it is not possible to find other players and their names.
            if (parsing.GetText(EntityManager, _proto, _random, ent.Owner) is null)
            {
                suitableAmbition = false;
                break;
            }
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

    private EntityPrototype? GenerateAmbition()
    {
        if (_ambitions.Count == 0)
        {
            Log.Error("No ambitions found");
            return null;
        }

        var totalWeight = 0f;
        foreach (var (_, weight) in _ambitions)
        {
            totalWeight += weight;
        }

        var randomValue = _random.NextFloat() * totalWeight;
        var currentWeight = 0f;

        foreach (var (prototype, weight) in _ambitions)
        {
            currentWeight += weight;
            if (randomValue <= currentWeight)
                return prototype;
        }

        return _ambitions[^1].prototype;
    }
}
