using Content.Shared.Polymorph;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Polymorph;

/// <summary>
/// Marks an entity as polymorphing into <see cref="Polymorph"/> whenever night falls
/// (<see cref="Content.Shared._CE.DayCycle.CEGlobalStartNightEvent"/>), and reverting back at
/// dawn - read by <see cref="Content.Server._CE.Polymorph.CENightPolymorphSystem"/>. Generic so any
/// feature (secret roles, curses, ...) can opt an entity into a day/night polymorph cycle just by
/// granting this component.
/// </summary>
[RegisterComponent]
public sealed partial class CENightPolymorphComponent : Component
{
    [DataField(required: true)]
    public ProtoId<PolymorphPrototype> Polymorph;
}
