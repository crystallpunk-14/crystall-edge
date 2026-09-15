using Content.Server.Mind;
using Content.Server.Roles;
using Content.Shared._CE.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Roles;

/// <summary>
/// Answers <see cref="CERequestSecretRoleInfoEvent"/> with the requesting player's current secret role,
/// read off their mind's <see cref="CESecretRoleComponent"/>. Kept separate from the vanilla
/// CharacterInfoEvent pipeline - see <see cref="CESecretRoleInfoEvent"/> for why.
/// </summary>
public sealed partial class CESecretRoleInfoSystem : EntitySystem
{
    [Dependency] private MindSystem _minds = default!;
    [Dependency] private RoleSystem _role = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<CERequestSecretRoleInfoEvent>(OnRequestSecretRoleInfo);
    }

    private void OnRequestSecretRoleInfo(CERequestSecretRoleInfoEvent msg, EntitySessionEventArgs args)
    {
        if (!args.SenderSession.AttachedEntity.HasValue
            || args.SenderSession.AttachedEntity != GetEntity(msg.NetEntity))
            return;

        var entity = args.SenderSession.AttachedEntity.Value;

        ProtoId<CESecretRolePrototype>? secretRole = null;
        if (_minds.TryGetMind(entity, out var mindId, out var mind)
            && _role.MindHasRole<CESecretRoleComponent>((mindId, mind), out var roleEnt))
        {
            secretRole = roleEnt.Value.Comp2.Role;
        }

        RaiseNetworkEvent(new CESecretRoleInfoEvent(GetNetEntity(entity), secretRole), args.SenderSession);
    }
}
