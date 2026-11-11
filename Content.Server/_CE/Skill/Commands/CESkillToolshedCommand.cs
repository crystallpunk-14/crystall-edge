using Content.Server.Administration;
using Content.Shared._CE.Skill.Prototypes;
using Content.Shared.Administration;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;

namespace Content.Server._CE.Skill.Commands;

/// <summary>
/// Toolshed skill commands: <c>self skill:add Fireball</c>, <c>entities with MindContainer skill:remove Fireball</c>.
/// </summary>
[ToolshedCommand(Name = "skill"), AdminCommand(AdminFlags.Admin)]
public sealed class CESkillToolshedCommand : ToolshedCommand
{
    private CESkillSystem? _skill;

    [CommandImplementation("add")]
    public EntityUid Add([PipedArgument] EntityUid input, ProtoId<CESkillPrototype> skill)
    {
        _skill ??= GetSys<CESkillSystem>();
        _skill.TryAddSkill(input, skill, force: true);
        return input;
    }

    [CommandImplementation("add")]
    public IEnumerable<EntityUid> Add([PipedArgument] IEnumerable<EntityUid> input, ProtoId<CESkillPrototype> skill)
    {
        foreach (var ent in input)
        {
            yield return Add(ent, skill);
        }
    }

    [CommandImplementation("remove")]
    public EntityUid Remove([PipedArgument] EntityUid input, ProtoId<CESkillPrototype> skill)
    {
        _skill ??= GetSys<CESkillSystem>();
        _skill.TryRemoveSkill(input, skill);
        return input;
    }

    [CommandImplementation("remove")]
    public IEnumerable<EntityUid> Remove([PipedArgument] IEnumerable<EntityUid> input, ProtoId<CESkillPrototype> skill)
    {
        foreach (var ent in input)
        {
            yield return Remove(ent, skill);
        }
    }
}
