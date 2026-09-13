using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.Roles;

/// <summary>
/// Sent by the client to ask what secret role (if any) the given entity's mind currently holds.
/// Kept as its own request/response pair instead of extending <c>CharacterInfoEvent</c>, since that
/// event is a record struct whose positional deconstruction is relied upon in several upstream files.
/// </summary>
[Serializable, NetSerializable]
public sealed class CERequestSecretRoleInfoEvent : EntityEventArgs
{
    public readonly NetEntity NetEntity;

    public CERequestSecretRoleInfoEvent(NetEntity netEntity)
    {
        NetEntity = netEntity;
    }
}

[Serializable, NetSerializable]
public sealed class CESecretRoleInfoEvent : EntityEventArgs
{
    public readonly NetEntity NetEntity;
    public readonly ProtoId<CESecretRolePrototype>? SecretRole;

    public CESecretRoleInfoEvent(NetEntity netEntity, ProtoId<CESecretRolePrototype>? secretRole)
    {
        NetEntity = netEntity;
        SecretRole = secretRole;
    }
}
