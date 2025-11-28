using Content.Shared._CE.Skill;
using Content.Shared._CE.Skill.Components;
using Content.Shared._CE.Skill.Prototypes;
using Robust.Client.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Client._CE.Skill;

public sealed partial class CEClientSkillSystem : CESharedSkillSystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public event Action<EntityUid>? OnSkillUpdate;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CESkillStorageComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
    }

    private void OnAfterAutoHandleState(Entity<CESkillStorageComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (ent != _playerManager.LocalEntity)
            return;

        OnSkillUpdate?.Invoke(ent.Owner);
    }

    public void RequestSkillData()
    {
        var localPlayer = _playerManager.LocalEntity;

        if (!HasComp<CESkillStorageComponent>(localPlayer))
            return;

        OnSkillUpdate?.Invoke(localPlayer.Value);
    }

    public void RequestLearnSkill(EntityUid? target, CESkillPrototype? skill)
    {
        if (skill == null || target == null)
            return;

        var netEv = new CETryLearnSkillMessage(GetNetEntity(target.Value), skill.ID);
        RaiseNetworkEvent(netEv);

        if (_proto.Resolve(skill.Tree, out var indexedTree))
        {
            _audio.PlayGlobal(indexedTree.LearnSound, target.Value, AudioParams.Default.WithVolume(6f));
        }
    }
}
