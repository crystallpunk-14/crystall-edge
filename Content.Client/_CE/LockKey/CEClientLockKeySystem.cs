using System.Text;
using Content.Client.Items;
using Content.Client.Stylesheets;
using Content.Shared._CE.LockKey;
using Content.Shared._CE.LockKey.Components;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client._CE.LockKey;

public sealed class CEClientLockKeySystem : CESharedLockKeySystem
{
    public override void Initialize()
    {
        base.Initialize();

        Subs.ItemStatus<CEKeyComponent>(ent => new CEKeyStatusControl(ent));
    }
}

public sealed class CEKeyStatusControl : Control
{
    private readonly Entity<CEKeyComponent> _parent;
    private readonly RichTextLabel _label;
    public CEKeyStatusControl(Entity<CEKeyComponent> parent)
    {
        _parent = parent;

        _label = new RichTextLabel { StyleClasses = { StyleClass.ItemStatus } };
        AddChild(_label);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        var sb = new StringBuilder("(");
        foreach (var item in _parent.Comp.Shape)
        {
            sb.Append($"{item} ");
        }

        sb.Append(")");
        _label.Text = sb.ToString();
    }
}
