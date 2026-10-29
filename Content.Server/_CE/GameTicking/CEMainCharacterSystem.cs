using System.Diagnostics.CodeAnalysis;
using Content.Server._CE.GameTicking.Components;
using Content.Shared.GameTicking;
using Robust.Shared.Network;

namespace Content.Server._CE.GameTicking;

/// <summary>
/// Tracks each player's <see cref="CEMainCharacterComponent"/> body.
/// </summary>
public sealed partial class CEMainCharacterSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        var character = EnsureComp<CEMainCharacterComponent>(ev.Mob);
        character.Player = ev.Player.UserId;
        character.Job = ev.JobId;
    }

    public bool TryGetMainCharacter(NetUserId player, [NotNullWhen(true)] out Entity<CEMainCharacterComponent>? character)
    {
        var query = EntityQueryEnumerator<CEMainCharacterComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Player != player)
                continue;

            character = (uid, comp);
            return true;
        }

        character = null;
        return false;
    }

    /// <summary>
    /// Makes <paramref name="newBody"/> the main character of <paramref name="oldBody"/>'s player,
    /// replacing whoever it belonged to before.
    /// </summary>
    public void TransferMainCharacter(EntityUid oldBody, EntityUid newBody)
    {
        if (!TryComp<CEMainCharacterComponent>(oldBody, out var old))
            return;

        var character = EnsureComp<CEMainCharacterComponent>(newBody);
        character.Player = old.Player;
        character.Job = old.Job;

        RemComp<CEMainCharacterComponent>(oldBody);
    }
}
