using Content.Shared._CE.Murk.Components;
using Content.Shared._CE.Murk.Systems;
using Content.Shared.CCVar;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;

namespace Content.Client._CE.Murk;

public sealed partial class CEClientMurkSystem : CESharedMurkSystem
{
    [Dependency] private IOverlayManager _overlayMgr = default!;
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private SharedTransformSystem _xform = default!;

    private readonly List<EntityUid> _cachedRemovalList = new();

    private const float FadeCutoff = 0.01f;

    public override void Initialize()
    {
        base.Initialize();

        _overlayMgr.AddOverlay(new CEMurkOverlay());
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlayMgr.RemoveOverlay<CEMurkOverlay>();
    }

    /// <summary>
    /// Smoothing lives here rather than in the overlay: the overlay only runs for maps that are
    /// currently being rendered, and runs once per z-level pass, so its speed used to depend both
    /// on framerate and on how many levels happened to be visible.
    /// </summary>
    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var rate = 1f - MathF.Exp(-_config.GetCVar(CCVars.CEMurkLerpRate) * frameTime);

        var maps = EntityQueryEnumerator<CEMurkedMapComponent>();
        while (maps.MoveNext(out var mapUid, out var murkedMap))
        {
            if (murkedMap.LerpInitialized)
            {
                murkedMap.LerpedIntensity = MathHelper.Lerp(murkedMap.LerpedIntensity, murkedMap.Intensity, rate);
            }
            else
            {
                murkedMap.LerpedIntensity = murkedMap.Intensity;
                murkedMap.LerpInitialized = true;
            }

            UpdateSourceBuffer((mapUid, murkedMap), rate);
        }
    }

    private void UpdateSourceBuffer(Entity<CEMurkedMapComponent> map, float rate)
    {
        var comp = map.Comp;
        comp.Seen.Clear();

        var query = EntityQueryEnumerator<CEMurkSourceComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var source, out var xform))
        {
            if (!source.Active)
                continue;

            if (!TryProjectSource(map.Owner, (uid, xform), source.Intensity, out var radius))
                continue;

            comp.Seen.Add(uid);
            var position = _xform.GetWorldPosition(uid);

            if (!comp.MurkBuffer.TryGetValue(uid, out var entry))
            {
                // Snap on first sight, so flying across the map does not send waves through the murk.
                comp.MurkBuffer[uid] = new CEMurkedMapComponent.MurkEntry
                {
                    Position = position,
                    Radius = radius,
                    Strength = source.Intensity,
                };
                continue;
            }

            entry.Position = position;
            entry.Radius = MathHelper.Lerp(entry.Radius, radius, rate);
            entry.Strength = MathHelper.Lerp(entry.Strength, source.Intensity, rate);
        }

        _cachedRemovalList.Clear();

        foreach (var (uid, entry) in comp.MurkBuffer)
        {
            if (comp.Seen.Contains(uid))
                continue;

            // Deleted, disabled or out of PVS - collapse the sphere instead of blinking it away.
            entry.Radius = MathHelper.Lerp(entry.Radius, 0f, rate);
            entry.Strength = MathHelper.Lerp(entry.Strength, 0f, rate);

            if (MathF.Abs(entry.Strength) < FadeCutoff)
                _cachedRemovalList.Add(uid);
        }

        foreach (var uid in _cachedRemovalList)
        {
            comp.MurkBuffer.Remove(uid);
        }
    }

    /// <summary>
    /// Flattens the smoothed buffer into the arrays handed to the shader.
    /// Returns false when the result would be fully transparent anyway.
    /// </summary>
    public bool BuildRenderBuffer(Entity<CEMurkedMapComponent> map)
    {
        var comp = map.Comp;
        comp.Count = 0;
        var anyMurk = comp.LerpedIntensity > 0f;

        foreach (var entry in comp.MurkBuffer.Values)
        {
            if (comp.Count >= CEMurkedMapComponent.MaxCount)
                break;

            if (entry.Radius <= 0f)
                continue;

            comp.Positions[comp.Count] = entry.Position;
            comp.Radii[comp.Count] = entry.Radius;
            comp.Strengths[comp.Count] = entry.Strength;
            comp.Count++;

            anyMurk |= entry.Strength > 0f;
        }

        return anyMurk;
    }
}
