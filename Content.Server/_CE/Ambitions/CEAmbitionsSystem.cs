using Content.Server.Administration;
using Content.Shared._CE.Ambitions;
using Content.Shared._CE.Ambitions.Prototypes;
using Content.Shared.Administration;
using Content.Shared.Objectives;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._CE.Ambitions;

public sealed class CEAmbitionsSystem : CESharedAmbitionsSystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private HashSet<CEAmbitionPrototype> _ambitions = new();

    public ObjectiveInfo? GenerateAmbition()
    {
        _ambitions.Clear();
        foreach (var a in _proto.EnumeratePrototypes<CEAmbitionPrototype>())
        {
            _ambitions.Add(a);
        }

        if (_ambitions.Count == 0)
        {
            Logger.Error("No ambitions found");
            return null;
        }

        var ambition = _random.Pick(_ambitions);

        var title = Loc.GetString(ambition.Name);
        var desc = Loc.GetString(ambition.Desc);

        foreach (var (key, parseEntry) in ambition.Parsings)
        {
            var parseKey = $"!{key}!";
            var parseValue = parseEntry.GetText(EntityManager, _proto, _random);

            title = title.Replace(parseKey, parseValue);
            desc = desc.Replace(parseKey, parseValue);
        }

        var objectiveInfo = new ObjectiveInfo(
            title,
            desc,
            ambition.Icon,
            1f);

        return objectiveInfo;
    }
}


[AdminCommand(AdminFlags.Server | AdminFlags.Mapping)]
public sealed class CEGetRandomAmbition : LocalizedEntityCommands
{
    [Dependency] private readonly CEAmbitionsSystem _ambitions = default!;

    public override string Command => "ambitionget";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var ambition = _ambitions.GenerateAmbition();

        if (ambition == null)
        {
            shell.WriteMarkup("ERROR: No ambition found");
            return;
        }

        shell.WriteMarkup($"TITLE: {ambition.Value.Title}");
        shell.WriteMarkup($"DESCRIPTION: {ambition.Value.Description}");
    }
}
