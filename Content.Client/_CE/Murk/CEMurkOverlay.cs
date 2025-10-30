using System.Numerics;
using Content.Client.Viewport;
using Content.Shared._CE.Murk.Components;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client._CE.Murk;

public sealed class CEMurkOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    private readonly SharedTransformSystem _transform;

    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    private readonly ShaderInstance? _murkShader;

    private Vector2 _playerPos = Vector2.Zero;

    private readonly EntityQuery<CEMurkedMapComponent> _mapQuery;

    private const float LerpStep = 0.01f;

    public CEMurkOverlay()
    {
        IoCManager.InjectDependencies(this);

        _murkShader = _proto.Index<ShaderPrototype>("CEMurk").InstanceUnique();
        _mapQuery = _entManager.GetEntityQuery<CEMurkedMapComponent>();

        _transform = _entManager.System<SharedTransformSystem>();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (args.Viewport.Eye == null)
            return false;

        if (!_mapQuery.TryGetComponent(args.MapUid, out var murkedMap))
            return false;

        _playerPos = args.Viewport.Eye.Position.Position;

        murkedMap.LerpedIntensity = MathHelper.Lerp(murkedMap.LerpedIntensity, murkedMap.Intensity, LerpStep);

        murkedMap.Seen.Clear();
        var query = _entManager.AllEntityQueryEnumerator<CEMurkSourceComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var murk, out var xform))
        {
            if (!murk.Active || xform.MapID != args.MapId)
                continue;

            murkedMap.Seen.Add(uid);

            var mapPos = _transform.GetWorldPosition(uid);

            if (!murkedMap.MurkBuffer.TryGetValue(uid, out var entry))
            {
                entry = new CEMurkedMapComponent.MurkEntry
                {
                    Intensity = 0,
                    Position = mapPos,
                };
                murkedMap.MurkBuffer[uid] = entry;
            }

            entry.Position = Vector2.Lerp(entry.Position, mapPos, LerpStep);
            entry.Intensity = MathHelper.Lerp(entry.Intensity, murk.Active ? murk.Intensity : 0, LerpStep);
        }

        var toRemove = new List<EntityUid>();
        foreach (var (uid, entry) in murkedMap.MurkBuffer)
        {
            if (!murkedMap.Seen.Contains(uid))
                entry.Intensity = MathHelper.Lerp(entry.Intensity, 0, LerpStep);

            if (Math.Abs(entry.Intensity) < 0.01f)
                toRemove.Add(uid);
        }

        foreach (var uid in toRemove)
        {
            murkedMap.MurkBuffer.Remove(uid);
        }

        murkedMap.Count = 0;
        foreach (var entry in murkedMap.MurkBuffer.Values)
        {
            if (murkedMap.Count >= CEMurkedMapComponent.MaxCount)
                break;

            var tempCoords = args.Viewport.WorldToLocal(entry.Position);
            tempCoords.Y = args.Viewport.Size.Y - tempCoords.Y;

            murkedMap.Positions[murkedMap.Count] = tempCoords;
            murkedMap.Intensities[murkedMap.Count] = entry.Intensity;
            murkedMap.Count++;
        }

        return true;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null || args.Viewport.Eye == null)
            return;

        var mapIntensity = 0f;
        if (!_mapQuery.TryGetComponent(args.MapUid, out var murkedMap))
            return;

        mapIntensity = murkedMap.LerpedIntensity;

        _murkShader?.SetParameter("renderScale", args.Viewport.RenderScale * args.Viewport.Eye.Scale);

        _murkShader?.SetParameter("baseIntensity", mapIntensity);
        _murkShader?.SetParameter("playerPos", _playerPos);
        _murkShader?.SetParameter("count", murkedMap.Count);
        _murkShader?.SetParameter("position", murkedMap.Positions);
        _murkShader?.SetParameter("intensities", murkedMap.Intensities);

        _murkShader?.SetParameter("SCREEN_TEXTURE", ScreenTexture);

        var worldHandle = args.WorldHandle;
        worldHandle.UseShader(_murkShader);
        worldHandle.DrawRect(args.WorldAABB, Color.White);
        worldHandle.UseShader(null);
    }
}

