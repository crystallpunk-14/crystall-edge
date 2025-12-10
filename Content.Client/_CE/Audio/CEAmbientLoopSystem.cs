using Content.Client.Audio;
using Content.Client.Gameplay;
using Content.Shared._CE.Audio.Prototypes;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Random.Rules;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Client._CE.Audio;

public sealed partial class CEAmbientLoopSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IStateManager _state = default!;
    [Dependency] private readonly RulesSystem _rules = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ContentAudioSystem _contentAudio = default!;

    private const float AmbientLoopFadeInTime = 1f;
    private const float AmbientLoopFadeOutTime = 4f;

    private Dictionary<CEAmbientLoopPrototype, EntityUid> _loopStreams = new();

    private TimeSpan _nextUpdateTime = TimeSpan.Zero;
    private readonly TimeSpan _updateFrequency = TimeSpan.FromSeconds(1f);

    private static float _volumeSlider;

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_configManager, CCVars.AmbientMusicVolume, AmbienceCVarChangedAmbientMusic, true);
        SubscribeNetworkEvent<RoundEndMessageEvent>(OnRoundEndMessage);
    }

    private void AmbienceCVarChangedAmbientMusic(float obj)
    {
        _volumeSlider = SharedAudioSystem.GainToVolume(obj);

        foreach (var loop in _loopStreams)
        {
            _audio.SetVolume(loop.Value, loop.Key.Sound.Params.Volume + _volumeSlider);
        }
    }

    private void OnRoundEndMessage(RoundEndMessageEvent ev)
    {
        foreach (var loop in _loopStreams)
        {
            StopAmbientLoop(loop.Key);
        }
    }

   public override void Update(float frameTime)
   {
       base.Update(frameTime);

       if (_timing.CurTime <= _nextUpdateTime)
           return;

       _nextUpdateTime = _timing.CurTime + _updateFrequency;

       if (_state.CurrentState is not GameplayState)
           return;

       var requiredLoops = GetAmbientLoops();

       foreach (var loop in _loopStreams)
       {
           if (!requiredLoops.Contains(loop.Key))  //If ambient is playing and it shouldn't, stop it.
               StopAmbientLoop(loop.Key);
       }

       foreach (var loop in requiredLoops)
       {
           if (!_loopStreams.ContainsKey(loop)) //If it's not playing, but should, run it
               StartAmbientLoop(loop);
       }
   }

   private void StartAmbientLoop(CEAmbientLoopPrototype proto)
   {
       if (_loopStreams.ContainsKey(proto))
           return;

       var newLoop = _audio.PlayGlobal(
           proto.Sound,
           Filter.Local(),
           false,
           AudioParams.Default
               .WithLoop(true)
               .WithVolume(proto.Sound.Params.Volume + _volumeSlider)
               .WithPlayOffset(_random.NextFloat(0f, 100f)));

       if (newLoop is null)
           return;

       _loopStreams.Add(proto, newLoop.Value.Entity);

       _contentAudio.FadeIn(newLoop.Value.Entity, newLoop.Value.Component, AmbientLoopFadeInTime);
   }

   private void StopAmbientLoop(CEAmbientLoopPrototype proto)
   {
       if (!_loopStreams.TryGetValue(proto, out var audioEntity))
           return;

       _contentAudio.FadeOut(audioEntity, duration: AmbientLoopFadeOutTime);
       _loopStreams.Remove(proto);
   }

   /// <summary>
   /// Checks the player's environment, and returns a list of all ambients that should currently be playing around the player
   /// </summary>
   /// <returns></returns>
   private List<CEAmbientLoopPrototype> GetAmbientLoops()
   {
       List<CEAmbientLoopPrototype> list = new();

       var player = _player.LocalEntity;

       if (player == null)
           return list;

       var ambientLoops = System.Linq.Enumerable.ToList(_proto.EnumeratePrototypes<CEAmbientLoopPrototype>());

       foreach (var loop in ambientLoops)
       {
           if (_rules.IsTrue(player.Value, _proto.Index(loop.Rules)))
           {
               list.Add(loop);
           }
       }

       return list;
   }
}
