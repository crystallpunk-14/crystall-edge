using Content.Shared._CE.Murk;
using Content.Shared._CE.Murk.Components;
using Content.Shared.CCVar;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;

namespace Content.Client._CE.Murk;

public sealed partial class CEClientMurkSystem : CESharedMurkSystem
{
    [Dependency] private IOverlayManager _overlayMgr = default!;
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private SharedTransformSystem _xform = default!;

    private readonly List<CEMurkedMapComponent.MurkKey> _cachedRemovalList = new();

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
            // Starts at zero even when the component appears mid-round, so murk rolls in
            // instead of snapping on.
            murkedMap.LerpedIntensity = MathHelper.Lerp(murkedMap.LerpedIntensity, murkedMap.Intensity, rate);

            UpdateSourceBuffer((mapUid, murkedMap), rate);
        }
    }

    private void UpdateSourceBuffer(Entity<CEMurkedMapComponent> map, float rate)
    {
        var comp = map.Comp;
        comp.Seen.Clear();

        var sources = EntityQueryEnumerator<CEMurkSourceComponent, TransformComponent>();
        while (sources.MoveNext(out var uid, out var source, out var xform))
        {
            if (source.Active)
                UpdateEntry(map, new CEMurkedMapComponent.MurkKey(uid, false), xform, source.Intensity, rate);
        }

        // A boundary clears the wall layer the same way a negative source clears ordinary murk.
        var boundaries = EntityQueryEnumerator<CEMurkBoundaryComponent, TransformComponent>();
        while (boundaries.MoveNext(out var uid, out var boundary, out var xform))
        {
            if (boundary.Active)
                UpdateEntry(map, new CEMurkedMapComponent.MurkKey(uid, true), xform, -boundary.Radius, rate);
        }

        _cachedRemovalList.Clear();

        foreach (var (key, entry) in comp.MurkBuffer)
        {
            if (comp.Seen.Contains(key))
                continue;

            // Deleted, disabled or out of PVS - collapse the sphere instead of blinking it away.
            entry.Radius = MathHelper.Lerp(entry.Radius, 0f, rate);
            entry.Strength = MathHelper.Lerp(entry.Strength, 0f, rate);

            if (MathF.Abs(entry.Strength) < FadeCutoff)
                _cachedRemovalList.Add(key);
        }

        foreach (var key in _cachedRemovalList)
        {
            comp.MurkBuffer.Remove(key);
        }
    }

    /// <summary>
    /// Projects a sphere of <paramref name="intensity"/> onto the map and smooths its buffer entry
    /// towards it. Spheres that don't reach the map stay unseen and collapse like removed ones.
    /// </summary>
    private void UpdateEntry(Entity<CEMurkedMapComponent> map, CEMurkedMapComponent.MurkKey key, TransformComponent xform, float intensity, float rate)
    {
        if (!TryProjectSource(map.Owner, (key.Uid, xform), intensity, out var radius))
            return;

        var comp = map.Comp;
        comp.Seen.Add(key);
        var position = _xform.GetWorldPosition(xform);

        if (!comp.MurkBuffer.TryGetValue(key, out var entry))
        {
            // Position is exact from the start, but the sphere itself grows out of nothing.
            entry = new CEMurkedMapComponent.MurkEntry { Position = position };
            comp.MurkBuffer[key] = entry;
        }

        entry.Position = position;
        entry.Radius = MathHelper.Lerp(entry.Radius, radius, rate);
        entry.Strength = MathHelper.Lerp(entry.Strength, intensity, rate);
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

        foreach (var (key, entry) in comp.MurkBuffer)
        {
            if (comp.Count >= CEMurkedMapComponent.MaxCount)
                break;

            if (entry.Radius <= 0f)
                continue;

            comp.Positions[comp.Count] = entry.Position;
            comp.Radii[comp.Count] = entry.Radius;
            comp.Strengths[comp.Count] = entry.Strength;
            comp.IsBoundary[comp.Count] = key.IsBoundary ? 1f : 0f;
            comp.Count++;

            anyMurk |= entry.Strength > 0f;
        }

        return anyMurk;
    }
}
