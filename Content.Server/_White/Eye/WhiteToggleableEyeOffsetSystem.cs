using Content.Server.Movement.Components;
using Content.Shared._White.Eye;

namespace Content.Server._White.Eye;

public sealed class WhiteToggleableEyeOffsetSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<EyeComponent, EyeOffsetToggleActionEvent>(OnToggleEyeOffset);
    }

    private void OnToggleEyeOffset(Entity<EyeComponent> ent, ref EyeOffsetToggleActionEvent args)
    {
        if (!HasComp<EyeCursorOffsetComponent>(ent))
            AddComp<EyeCursorOffsetComponent>(ent);
        else
            RemComp<EyeCursorOffsetComponent>(ent);
    }
}
