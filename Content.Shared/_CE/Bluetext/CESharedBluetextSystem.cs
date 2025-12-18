
using Content.Shared.Mind;

namespace Content.Shared._CE.Bluetext;

public abstract class CESharedBlueTextSystem : EntitySystem
{
    [Dependency] protected readonly SharedMindSystem Mind = default!;
}
