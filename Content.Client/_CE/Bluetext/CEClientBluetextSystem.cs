using Content.Client.CharacterInfo;

namespace Content.Client._CE.Bluetext;

public sealed class CEClientBluetextSystem : EntitySystem
{
    [Dependency] private readonly CharacterInfoSystem _characterInfo = default!;

    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<CharacterInfoSystem.GetCharacterInfoControlsEvent>(OnGetCharacterInfoControls);
    }

    private void OnGetCharacterInfoControls(CharacterInfoSystem.GetCharacterInfoControlsEvent ev)
    {
        throw new NotImplementedException();
    }
}
