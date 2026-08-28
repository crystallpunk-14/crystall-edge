using Content.Shared._CE.Power.PowerMonitoring;
using Content.Shared.Power;
using Robust.Client.UserInterface;

namespace Content.Client._CE.Power.PowerMonitoring;

public sealed class CEPowerMonitoringConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private CEPowerMonitoringWindow? _menu;

    public CEPowerMonitoringConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<CEPowerMonitoringWindow>();
        _menu.SetEntity(Owner);
        _menu.SendPowerMonitoringConsoleMessageAction += SendPowerMonitoringConsoleMessage;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        var castState = (CEPowerMonitoringConsoleBoundInterfaceState) state;

        _menu?.ShowEntities(
            castState.TotalSources,
            castState.TotalBatteryUsage,
            castState.TotalLoads,
            castState.AllEntries,
            castState.FocusSources,
            castState.FocusLoads);
    }

    public void SendPowerMonitoringConsoleMessage(NetEntity? netEntity, PowerMonitoringConsoleGroup group)
    {
        SendMessage(new CEPowerMonitoringConsoleMessage(netEntity, group));
    }
}
