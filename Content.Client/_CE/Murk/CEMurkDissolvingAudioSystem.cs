using Content.Shared._CE.Murk.Components;
using Content.Shared.CCVar;
using Robust.Client.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Client._CE.Murk;

/// <summary>
/// Plays looping drones locally for the dissolving player, getting louder the more dissolved they are.
/// </summary>
public sealed partial class CEMurkDissolvingAudioSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    /// <summary>
    /// Base layer, creeping in as soon as the entity starts dissolving.
    /// </summary>
    private static readonly SoundSpecifier MurkSound = new SoundPathSpecifier("/Audio/_CE/Ambience/Loops/murk.ogg");

    /// <summary>
    /// Second layer, only kicking in once dissolution is nearly complete.
    /// </summary>
    private static readonly SoundSpecifier DreadSound = new SoundPathSpecifier("/Audio/Ambience/anomaly_scary.ogg");

    /// <summary>
    /// Volume of each layer at its full strength, in dB. Lower dissolution scales down from here.
    /// </summary>
    private const float MurkMaxVolume = -4f;
    private const float DreadMaxVolume = 0f;

    /// <summary>
    /// Dissolution level at which the dread layer starts fading in.
    /// </summary>
    private const float DreadStart = 0.75f;

    private EntityUid? _murkStream;
    private EntityUid? _dreadStream;
    private float _ambienceVolume;

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_cfg, CCVars.AmbienceVolume, OnAmbienceVolumeChanged, true);
    }

    private void OnAmbienceVolumeChanged(float value)
    {
        _ambienceVolume = SharedAudioSystem.GainToVolume(value);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var dissolved = GetLocalDissolved();
        var dread = Math.Clamp((dissolved - DreadStart) / (1f - DreadStart), 0f, 1f);

        _murkStream = UpdateLayer(_murkStream, MurkSound, dissolved, MurkMaxVolume);
        _dreadStream = UpdateLayer(_dreadStream, DreadSound, dread, DreadMaxVolume);
    }

    /// <summary>
    /// Keeps a single looping layer in sync with its strength, starting and stopping it as needed.
    /// <paramref name="strength"/> doubles as the gain, so the layer creeps in instead of snapping
    /// to full volume.
    /// </summary>
    private EntityUid? UpdateLayer(EntityUid? stream, SoundSpecifier sound, float strength, float maxVolume)
    {
        if (strength <= 0f)
        {
            Stop(stream);
            return null;
        }

        // The stream is client-side, so it can go away under us - respawn it if it did.
        if (!Exists(stream))
            stream = _audio.PlayGlobal(sound, Filter.Local(), false, AudioParams.Default.WithLoop(true))?.Entity;

        _audio.SetVolume(stream, maxVolume + SharedAudioSystem.GainToVolume(strength) + _ambienceVolume);
        return stream;
    }

    private float GetLocalDissolved()
    {
        if (!TryComp<CEMurkDissolvingComponent>(_player.LocalEntity, out var dissolving) || !dissolving.Enabled)
            return 0f;

        return dissolving.Dissolved;
    }

    private void Stop(EntityUid? stream)
    {
        if (stream == null)
            return;

        _audio.Stop(stream);
    }
}
