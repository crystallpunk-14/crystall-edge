using System.Numerics;
using Content.Client.Resources;
using Content.Shared._CE.LockKey;
using Content.Shared._CE.LockKey.Components;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Client._CE.LockKey.UI;

/// <summary>
/// Pixel-art lockpicking view: pins with progress indicators, the lockpick and the press power column.
/// All coordinates are in source texture pixels.
/// </summary>
public sealed partial class CELockpickControl : Control
{
    [Dependency] private IEntityManager _entMan = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IResourceCache _cache = default!;

    private const string TexturePath = "/Textures/_CE/Interface/Lockpicking/";
    private const int PixelScale = 4;

    // Geometry matched to the frame sprites.
    private const int OffsetX = 4; // room for the lockpick handle left of the frame
    private const int Height = 64;
    private const int FrameMiddleX = 16;
    private const int FrameRightBaseX = 21;
    private const int FrameRightWidth = 63;
    private const int PinStep = 15;
    private const int PinX = 2;
    private const int PinY = 14;
    private const int PinCenterX = 15;
    private const int IndicatorX = 13;
    private const int IndicatorY = 12;
    private const int LockpickY = 40;
    private const int LockpickHookX = 135;
    private const int PanelX = 36;
    private const int PowerY = 9;
    private const int PowerStep = 10;
    private const int FrameSize = 32;

    // Bottom pixel row of the pin per animation frame, the lockpick hook follows it.
    private static readonly int[] MoveBottom = [28, 26, 23, 21];
    private static readonly int[] FailBottom = [28, 26, 25, 26, 28];
    private const int PinBottom = 28;

    private static readonly TimeSpan FailTime = TimeSpan.FromSeconds(0.5f);
    private const float FailJitterSeconds = 0.16f;
    private static readonly TimeSpan MoveHoldTime = TimeSpan.FromSeconds(0.5f);

    private readonly Texture _frameLeft;
    private readonly Texture _frameMiddle;
    private readonly Texture _frameRight;
    private readonly Texture _pin;
    private readonly Texture _pinPicked;
    private readonly Texture _pinMove;
    private readonly Texture _pinFailed;
    private readonly Texture _power;
    private readonly Texture _powerSelected;
    private readonly Texture _lockpick;
    private readonly Texture _done;
    private readonly Texture _select;

    private EntityUid _lock;
    private int _seenStatus;
    private int _seenFailures;
    private int? _hover;

    private AnimKind _anim = AnimKind.None;
    private TimeSpan _animStart;
    private int _animPower;
    private int _failFrom;
    private readonly List<float> _failDelays = new();

    public event Action<int>? OnPowerPressed;

    public CELockpickControl()
    {
        IoCManager.InjectDependencies(this);

        _frameLeft = _cache.GetTexture(TexturePath + "part1.png");
        _frameMiddle = _cache.GetTexture(TexturePath + "part2.png");
        _frameRight = _cache.GetTexture(TexturePath + "part3.png");
        _pin = _cache.GetTexture(TexturePath + "pin.png");
        _pinPicked = _cache.GetTexture(TexturePath + "pin_doned.png");
        _pinMove = _cache.GetTexture(TexturePath + "pin_move.png");
        _pinFailed = _cache.GetTexture(TexturePath + "pin_failed.png");
        _power = _cache.GetTexture(TexturePath + "power.png");
        _powerSelected = _cache.GetTexture(TexturePath + "power_selected.png");
        _lockpick = _cache.GetTexture(TexturePath + "lockpick.png");
        _done = _cache.GetTexture(TexturePath + "done.png");
        _select = _cache.GetTexture(TexturePath + "select.png");

        MouseFilter = MouseFilterMode.Stop;
        RectClipContent = true;
    }

    public void SetLock(EntityUid lockEnt)
    {
        _lock = lockEnt;
        if (_entMan.TryGetComponent<CELockComponent>(lockEnt, out var comp))
        {
            _seenStatus = comp.LockPickStatus;
            _seenFailures = comp.LockPickFailures;
        }

        InvalidateMeasure();
    }

    private float Scale => PixelScale * UIScale;

    private static int FrameRightX(int pins) => OffsetX + FrameRightBaseX + PinStep * (pins - 2);

    private static int SlotX(int pin) => OffsetX + PinX + PinStep * pin;

    private int PinCount => _entMan.TryGetComponent<CELockComponent>(_lock, out var comp) ? Math.Max(2, comp.Shape.Count) : 5;

    protected override Vector2 MeasureOverride(Vector2 availableSize)
    {
        return new Vector2(FrameRightX(PinCount) + FrameRightWidth, Height) * PixelScale;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (!_entMan.TryGetComponent<CELockComponent>(_lock, out var comp))
            return;

        var now = _timing.RealTime;

        if (comp.LockPickFailures != _seenFailures)
        {
            _seenFailures = comp.LockPickFailures;
            StartFail(_seenStatus, now);
        }
        else if (comp.LockPickStatus > _seenStatus && _anim == AnimKind.Move)
        {
            _anim = AnimKind.None;
        }

        _seenStatus = comp.LockPickStatus;

        // Drop a press that never got a result, e.g. the DoAfter did not start.
        if (_anim == AnimKind.Move && now - _animStart > GetHackTime(comp) + MoveHoldTime)
            _anim = AnimKind.None;

        if (_anim == AnimKind.Fail && now - _animStart > FailTime + TimeSpan.FromSeconds(FailJitterSeconds))
            _anim = AnimKind.None;
    }

    private void StartFail(int raisedPins, TimeSpan now)
    {
        _anim = AnimKind.Fail;
        _animStart = now;
        _failFrom = raisedPins;

        // Raised pins drop with a small random spread, the current one drops first.
        _failDelays.Clear();
        for (var i = 0; i <= raisedPins; i++)
        {
            _failDelays.Add(i == raisedPins ? 0f : _random.NextFloat() * FailJitterSeconds);
        }
    }

    private TimeSpan GetHackTime(CELockComponent comp)
    {
        return _entMan.TryGetComponent<CELockpickComponent>(comp.Lockpick, out var lockpick)
            ? lockpick.HackTime
            : TimeSpan.Zero;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        if (!_entMan.TryGetComponent<CELockComponent>(_lock, out var comp) || comp.Shape.Count == 0)
            return;

        var pins = comp.Shape.Count;
        var elapsed = _timing.RealTime - _animStart;

        // Frame
        DrawTexture(handle, _frameLeft, OffsetX, 0);
        for (var i = 0; i < pins - 2; i++)
        {
            DrawTexture(handle, _frameMiddle, OffsetX + FrameMiddleX + PinStep * i, 0);
        }

        var panelX = FrameRightX(pins);
        DrawTexture(handle, _frameRight, panelX, 0);

        // Pins and indicators
        var status = _anim == AnimKind.Fail ? _failFrom : comp.LockPickStatus;
        var bottom = PinBottom;
        for (var pin = 0; pin < pins; pin++)
        {
            var x = SlotX(pin);
            var current = pin == status;
            var raised = pin < status;

            if (current && _anim == AnimKind.Move)
            {
                var hackTime = GetHackTime(comp);
                var progress = hackTime > TimeSpan.Zero ? Math.Clamp(elapsed / hackTime, 0, 1) : 1;
                var frame = Math.Min(MoveBottom.Length - 1, (int) (progress * MoveBottom.Length));
                DrawFrame(handle, _pinMove, frame, x, PinY);
                bottom = MoveBottom[frame];
            }
            else if (_anim == AnimKind.Fail && pin <= _failFrom)
            {
                var t = elapsed.TotalSeconds - _failDelays[pin];
                if (t < 0)
                {
                    DrawTexture(handle, _pinPicked, x, PinY);
                }
                else
                {
                    raised = false;
                    var frame = Math.Min(FailBottom.Length - 1, (int) (t / FailTime.TotalSeconds * FailBottom.Length));
                    DrawFrame(handle, _pinFailed, frame, x, PinY);
                    if (current)
                        bottom = FailBottom[frame];
                }
            }
            else
            {
                DrawTexture(handle, raised ? _pinPicked : _pin, x, PinY);
            }

            if (raised)
                DrawTexture(handle, _done, x + IndicatorX, IndicatorY);
            else if (current)
                DrawTexture(handle, _select, x + IndicatorX, IndicatorY);
        }

        // The lockpick hook sits under the current pin and rides up with it.
        var hookX = SlotX(Math.Min(status, pins - 1)) + PinCenterX;
        DrawTexture(handle, _lockpick, hookX - LockpickHookX, LockpickY - (PinBottom - bottom));

        // Power column, top is the strongest press.
        var lit = _anim == AnimKind.Move ? _animPower : _hover;
        for (var i = 0; i < CESharedLockKeySystem.MaxHeight; i++)
        {
            var power = CESharedLockKeySystem.MaxHeight - i;
            DrawTexture(handle, power == lit ? _powerSelected : _power, panelX + PanelX, PowerY + PowerStep * i);
        }
    }

    private void DrawTexture(DrawingHandleScreen handle, Texture texture, int x, int y)
    {
        handle.DrawTextureRect(texture, UIBox2.FromDimensions(new Vector2(x, y) * Scale, new Vector2(texture.Width, texture.Height) * Scale));
    }

    private void DrawFrame(DrawingHandleScreen handle, Texture texture, int frame, int x, int y)
    {
        var region = new UIBox2(frame * FrameSize, 0, (frame + 1) * FrameSize, FrameSize);
        var rect = UIBox2.FromDimensions(new Vector2(x, y) * Scale, new Vector2(FrameSize, FrameSize) * Scale);
        handle.DrawTextureRectRegion(texture, rect, region);
    }

    private int? PowerAt(Vector2 pixelPosition)
    {
        var pos = pixelPosition / Scale;
        var x = FrameRightX(PinCount) + PanelX;
        if (pos.X < x || pos.X >= x + _power.Width)
            return null;

        for (var i = 0; i < CESharedLockKeySystem.MaxHeight; i++)
        {
            var y = PowerY + PowerStep * i;
            if (pos.Y >= y && pos.Y < y + PowerStep)
                return CESharedLockKeySystem.MaxHeight - i;
        }

        return null;
    }

    protected override void MouseMove(GUIMouseMoveEventArgs args)
    {
        base.MouseMove(args);
        _hover = PowerAt(args.RelativePixelPosition);
    }

    protected override void MouseExited()
    {
        base.MouseExited();
        _hover = null;
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        if (args.Function != EngineKeyFunctions.UIClick || _anim != AnimKind.None)
            return;

        if (PowerAt(args.RelativePixelPosition) is not { } power)
            return;

        args.Handle();
        _anim = AnimKind.Move;
        _animStart = _timing.RealTime;
        _animPower = power;
        OnPowerPressed?.Invoke(power);
    }

    private enum AnimKind : byte
    {
        None,
        Move,
        Fail,
    }
}
