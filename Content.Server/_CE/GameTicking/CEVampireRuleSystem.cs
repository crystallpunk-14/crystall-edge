using Content.Server._CE.GameTicking.Components;
using Content.Server._CE.Objectives.Systems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.GameTicking;

public sealed class CEVampireRuleSystem : GameRuleSystem<CEVampireRuleComponent>
{
    [Dependency] private readonly CEVampireObjectiveConditionsSystem _condition = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    protected override void AppendRoundEndText(EntityUid uid,
        CEVampireRuleComponent component,
        GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(uid, component, gameRule, ref args);

        /* TODO: This code will be implemmented soon. I dont wanna delete it as it contains important logic for end of round text.
        //Alive percentage
        var alivePercentage = _condition.CalculateAlivePlayersPercentage();

        var query = EntityQueryEnumerator<CEVampireComponent>();
        while (query.MoveNext(out _, out var heart))
        {
            if (heart.Level < _condition.RequiredHeartLevel)
                continue;

            aliveFactions.Add(heart.Faction.Value);
        }

        args.AddLine($"[head=2][color=#ab1b3d]{Loc.GetString("ce-vampire-clans-battle")}[/color][/head]");

            if (aliveFactions.Count == 0)
            {
                //City win
                args.AddLine($"[head=3][color=#7d112b]{Loc.GetString("ce-vampire-clans-battle-clan-city-win")}[/color][/head]");
                args.AddLine(Loc.GetString("ce-vampire-clans-battle-clan-city-win-desc"));
            }

            if (aliveFactions.Count == 1)
            {
                var faction = aliveFactions.First();

                if (_proto.TryIndex(faction, out var indexedFaction))
                    args.AddLine($"[head=3][color=#7d112b]{Loc.GetString("ce-vampire-clans-battle-clan-win", ("name", Loc.GetString(indexedFaction.Name)))}[/color][/head]");
                args.AddLine(Loc.GetString("ce-vampire-clans-battle-clan-win-desc"));
            }

            if (aliveFactions.Count == 2)
            {
                var factions = aliveFactions.ToArray();

                if (_proto.TryIndex(factions[0], out var indexedFaction1) && _proto.TryIndex(factions[1], out var indexedFaction2))
                    args.AddLine($"[head=3][color=#7d112b]{Loc.GetString("ce-vampire-clans-battle-clan-tie-2", ("name1", Loc.GetString(indexedFaction1.Name)), ("name2", Loc.GetString(indexedFaction2.Name)))}[/color][/head]");
                args.AddLine(Loc.GetString("ce-vampire-clans-battle-clan-tie-2-desc"));
            }

            if (aliveFactions.Count >= 3)
            {
                args.AddLine($"[head=3][color=#7d112b]{Loc.GetString("ce-vampire-clans-battle-clan-tie-3")}[/color][/head]");
                args.AddLine(Loc.GetString("ce-vampire-clans-battle-clan-tie-3-desc"));
            }


        args.AddLine(Loc.GetString("ce-vampire-clans-battle-alive-people", ("percent", MathF.Round(alivePercentage * 100))));
        */
    }
}
