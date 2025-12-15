
using Content.Shared._CE.Input;
using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Shared._CE.ZLevels.Core.EntitySystems;

public abstract partial class CESharedZLevelsSystem
{
    private void InitBind()
    {
        CommandBinds.Builder
            .Bind(CEContentKeyFunctions.SelectedZLayerUp, InputCmdHandler.FromDelegate(HandleSelectedZLayerUp))
            .Bind(CEContentKeyFunctions.SelectedZLayerDown, InputCmdHandler.FromDelegate(HandleSelectedZLayerDown))
            .Bind(CEContentKeyFunctions.SelectedZLayerReset, InputCmdHandler.FromDelegate(HandleSelectedZLayerReset))
            .Bind(CEContentKeyFunctions.ToggleZLayerRelation, InputCmdHandler.FromDelegate(HandleToggleZLayerRelation))
            .Register<CESharedZLevelsSystem>();
    }

    private void HandleSelectedZLayerReset(ICommonSession? playerSession)
    {
        if (playerSession?.AttachedEntity is not { Valid: true } player || !Exists(player))
            return;
        if (!TryComp<CEZLevelViewerComponent>(player, out var comp))
            return;

        var newvalue = 0;

        RaisePredictiveEvent<ChangeViewedZLayerEvent>(new(GetNetEntity(player), newvalue));
    }
    private void HandleSelectedZLayerUp(ICommonSession? playerSession)
    {
        if (playerSession?.AttachedEntity is not { Valid: true } player || !Exists(player))
            return;
        if (!TryComp<CEZLevelViewerComponent>(player, out var comp))
            return;

        var oldvalue = comp!.ViewedZLevel;
        var newvalue = oldvalue + 1;

        RaisePredictiveEvent<ChangeViewedZLayerEvent>(new(GetNetEntity(player), newvalue));
    }
    private void HandleSelectedZLayerDown(ICommonSession? playerSession)
    {
        if (playerSession?.AttachedEntity is not { Valid: true } player || !Exists(player))
            return;

        if (!TryComp<CEZLevelViewerComponent>(player, out var comp))
            return;

        var oldvalue = comp!.ViewedZLevel;
        var newvalue = oldvalue - 1;

        RaisePredictiveEvent<ChangeViewedZLayerEvent>(new(GetNetEntity(player), newvalue));
    }
    //todo
    private void HandleToggleZLayerRelation(ICommonSession? playerSession)
    {
        if (playerSession?.AttachedEntity is not { Valid: true } player || !Exists(player))
            return;
        if (!TryComp<CEZLevelViewerComponent>(player, out var comp))
            return;


    }
}
