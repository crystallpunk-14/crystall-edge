/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using Content.Shared.Stacks;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Workbench.Prototypes;

/// <summary>
/// Allows you to group several different kinds of stacks into one group. Can be used for situations where different stacks are appropriate for a particular situation
/// </summary>
[Prototype("CEStackGroup")]
public sealed partial class CEStackGroupPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    [DataField(required: true)]
    public List<ProtoId<StackPrototype>> Stacks = new();
}
