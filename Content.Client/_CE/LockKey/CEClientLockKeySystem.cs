using System.Text;
using Content.Client.Items;
using Content.Client.Stylesheets;
using Content.Shared._CE.LockKey.Components;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client._CE.LockKey;

public sealed class CEClientLockKeySystem : EntitySystem
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

        _label = new RichTextLabel { StyleClasses = { StyleNano.StyleClassItemStatus } };
        AddChild(_label);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (_parent.Comp.LockShape is null)
            return;

        var sb = new StringBuilder("(");
        foreach (var item in _parent.Comp.LockShape)
        {
            sb.Append($"{item} ");
        }

        sb.Append(")");
        _label.Text = sb.ToString();
    }
}
