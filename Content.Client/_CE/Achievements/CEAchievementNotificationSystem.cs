using Content.Shared._CE.Achievements;
using Robust.Client.Audio;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Client._CE.Achievements;

/// <summary>
/// Client-side system that handles achievement unlock notifications and displays them as popups.
/// </summary>
public sealed partial class CEAchievementNotificationSystem : EntitySystem
{
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private IUserInterfaceManager _userInterface = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IPrototypeManager _protoManager = default!;

    private SoundSpecifier _notificationSound = new SoundPathSpecifier("/Audio/_CE/achievement.ogg");

    private CEAchievementNotificationControl _ui = default!;
    private bool _remove;

    private readonly Queue<CEAchievementUnlockedEvent> _queue = new();
    private bool _isPlaying;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<CEAchievementUnlockedEvent>(OnAchievementUnlocked);

        _ui = new CEAchievementNotificationControl();
        _ui.OnAnimationEnd += OnAnimationEnd;
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (!_remove)
            return;

        _userInterface.RootControl.RemoveChild(_ui);
        _remove = false;
    }

    private void OnAchievementUnlocked(CEAchievementUnlockedEvent ev)
    {
        if (_player.LocalEntity is null)
            return;

        _queue.Enqueue(ev);

        if (!_isPlaying)
            PlayNext();
    }

    private void OnAnimationEnd()
    {
        PlayNext();
    }

    private void PlayNext()
    {
        if (_queue.Count == 0)
        {
            _isPlaying = false;
            _remove = true;
            return;
        }

        var ev = _queue.Dequeue();

        // Get achievement prototype for display data
        if (!_protoManager.Resolve(ev.AchievementProtoId, out var achievement))
        {
            Log.Error($"Failed to find achievement prototype: {ev.AchievementProtoId}");
            PlayNext(); // Skip this one and try next
            return;
        }

        if (_player.LocalEntity is not null)
            _audio.PlayGlobal(_notificationSound, _player.LocalEntity.Value);

        if (_ui.Parent is null)
            _userInterface.RootControl.AddChild(_ui);

        _remove = false;
        _isPlaying = true;
        _ui.AnimationStart(achievement, ev.Percentage);
    }
}
