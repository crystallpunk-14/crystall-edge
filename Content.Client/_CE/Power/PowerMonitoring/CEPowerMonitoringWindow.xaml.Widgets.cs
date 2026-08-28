using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Content.Client.Stylesheets;
using Content.Shared._CE.Power.PowerMonitoring;
using Content.Shared.Power;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client._CE.Power.PowerMonitoring;

public sealed partial class CEPowerMonitoringWindow
{
    private readonly SpriteSpecifier.Texture _sourceIcon =
        new(new ResPath("/Textures/Interface/PowerMonitoring/source_arrow.png"));

    private readonly SpriteSpecifier.Texture _loadIconPath =
        new(new ResPath("/Textures/Interface/PowerMonitoring/load_arrow.png"));

    private bool _autoScrollActive;
    private bool _autoScrollAwaitsUpdate;

    private void UpdateWindowConsoleEntry(
        BoxContainer masterContainer,
        int index,
        PowerMonitoringConsoleEntry entry,
        PowerMonitoringConsoleEntry[] focusSources,
        PowerMonitoringConsoleEntry[] focusLoads)
    {
        UpdateWindowConsoleEntry(masterContainer, index, entry);

        if (masterContainer.GetChild(index) is not CEPowerMonitoringWindowEntry windowEntry)
            return;

        UpdateEntrySourcesOrLoads(masterContainer, windowEntry.SourcesContainer, focusSources, _sourceIcon);
        UpdateEntrySourcesOrLoads(masterContainer, windowEntry.LoadsContainer, focusLoads, _loadIconPath);

        windowEntry.MainContainer.Visible = true;
    }

    private void UpdateWindowConsoleEntry(BoxContainer masterContainer, int index, PowerMonitoringConsoleEntry entry)
    {
        CEPowerMonitoringWindowEntry? windowEntry;

        if (index >= masterContainer.ChildCount)
        {
            windowEntry = new CEPowerMonitoringWindowEntry(entry);
            masterContainer.AddChild(windowEntry);

            windowEntry.Button.OnButtonUp += _ =>
            {
                windowEntry.SourcesContainer.RemoveAllChildren();
                windowEntry.LoadsContainer.RemoveAllChildren();
                ButtonAction(windowEntry, masterContainer);
            };
        }
        else
        {
            windowEntry = masterContainer.GetChild(index) as CEPowerMonitoringWindowEntry;
        }

        if (windowEntry == null)
            return;

        windowEntry.NetEntity = entry.NetEntity;
        windowEntry.Entry = entry;
        windowEntry.MainContainer.Visible = false;

        UpdateWindowEntryButton(entry.NetEntity, windowEntry.Button, entry);
    }

    public void UpdateWindowEntryButton(NetEntity netEntity,
        CEPowerMonitoringButton button,
        PowerMonitoringConsoleEntry entry)
    {
        if (!netEntity.IsValid() || entry.MetaData == null)
            return;

        if (netEntity == _focusEntity)
            button.AddStyleClass(StyleClass.Positive);
        else
            button.RemoveStyleClass(StyleClass.Positive);

        if (entry.MetaData.Value.SpritePath != string.Empty && entry.MetaData.Value.SpriteState != string.Empty)
        {
            button.TextureRect.Texture =
                _spriteSystem.Frame0(new SpriteSpecifier.Rsi(new ResPath(entry.MetaData.Value.SpritePath),
                    entry.MetaData.Value.SpriteState));
        }

        var name = entry.MetaData.Value.EntityName;
        button.NameLocalized.Text = name;
        button.ToolTip = name;

        button.PowerValue.Text = Loc.GetString("ce-power-monitoring-window-button-value",
            ("value", Math.Round(entry.PowerValue).ToString("N0")));

        if (entry.BatteryLevel != null)
        {
            button.BatteryLevel.Value = entry.BatteryLevel.Value;
            button.BatteryLevel.Visible = true;

            button.BatteryPercentage.Text = entry.BatteryLevel.Value.ToString("P0");
            button.BatteryPercentage.Visible = true;

            var color = Color.FromHsv(new Vector4(entry.BatteryLevel.Value * 0.33f, 1, 1, 1));
            button.BatteryLevel.ForegroundStyleBoxOverride = new StyleBoxFlat { BackgroundColor = color };
        }
        else
        {
            button.BatteryLevel.Visible = false;
            button.BatteryPercentage.Visible = false;
        }
    }

    private void UpdateEntrySourcesOrLoads(BoxContainer masterContainer,
        BoxContainer? currentContainer,
        PowerMonitoringConsoleEntry[]? entries,
        SpriteSpecifier.Texture icon)
    {
        if (currentContainer == null)
            return;

        if (entries == null || entries.Length == 0)
        {
            currentContainer.RemoveAllChildren();
            return;
        }

        while (currentContainer.ChildCount > entries.Length)
        {
            currentContainer.RemoveChild(currentContainer.GetChild(currentContainer.ChildCount - 1));
        }

        while (currentContainer.ChildCount < entries.Length)
        {
            var entry = entries[currentContainer.ChildCount];
            var subEntry = new CEPowerMonitoringWindowSubEntry(entry);
            currentContainer.AddChild(subEntry);

            subEntry.Button.OnButtonUp += _ => ButtonAction(subEntry, masterContainer);
        }

        foreach (var child in currentContainer.Children)
        {
            if (child is not CEPowerMonitoringWindowSubEntry castChild)
                continue;

            if (castChild.Icon != null)
                castChild.Icon.Texture = _spriteSystem.Frame0(icon);

            var entry = entries[child.GetPositionInParent()];

            castChild.NetEntity = entry.NetEntity;
            castChild.Entry = entry;

            UpdateWindowEntryButton(entry.NetEntity, castChild.Button, entries.ElementAt(child.GetPositionInParent()));
        }
    }

    private void ButtonAction(CEPowerMonitoringWindowBaseEntry entry, BoxContainer masterContainer)
    {
        if (entry.NetEntity == _focusEntity)
        {
            entry.Button.RemoveStyleClass(StyleClass.Positive);
            _focusEntity = null;

            SendPowerMonitoringConsoleMessageAction?.Invoke(null, entry.Entry.Group);
            return;
        }

        entry.Button.AddStyleClass(StyleClass.Positive);
        ActivateAutoScrollToFocus();

        if (_focusEntity != null)
        {
            foreach (CEPowerMonitoringWindowEntry sibling in masterContainer.Children)
            {
                if (sibling.NetEntity == _focusEntity)
                {
                    sibling.Button.RemoveStyleClass(StyleClass.Positive);
                    break;
                }
            }
        }

        _focusEntity = entry.NetEntity;

        if (NavMap.TrackedEntities.TryGetValue(entry.NetEntity, out var blip))
            NavMap.CenterToCoordinates(blip.Coordinates);

        SwitchTabsBasedOnPowerMonitoringConsoleGroup(entry.Entry.Group);
        SendPowerMonitoringConsoleMessageAction?.Invoke(_focusEntity, entry.Entry.Group);
    }

    private void ActivateAutoScrollToFocus()
    {
        _autoScrollActive = false;
        _autoScrollAwaitsUpdate = true;
    }

    private bool TryGetNextScrollPosition([NotNullWhen(true)] out float? nextScrollPosition)
    {
        nextScrollPosition = null;

        if (MasterTabContainer.Children.ElementAt(MasterTabContainer.CurrentTab) is not ScrollContainer scroll)
            return false;

        if (scroll.Children.ElementAt(0) is not BoxContainer container || !container.Children.Any())
            return false;

        if (!container.Children.Any(x => x.Height > 0))
            return false;

        nextScrollPosition = 0;

        foreach (var control in container.Children)
        {
            if (control is not CEPowerMonitoringWindowEntry entry)
                continue;

            if (entry.NetEntity == _focusEntity)
                return true;

            nextScrollPosition += control.Height;
        }

        nextScrollPosition = null;
        return false;
    }

    private void AutoScrollToFocus()
    {
        if (!_autoScrollActive)
            return;

        if (MasterTabContainer.Children.ElementAt(MasterTabContainer.CurrentTab) is not ScrollContainer scroll)
            return;

        if (!TryGetNextScrollPosition(out var nextScrollPosition))
            return;

        scroll.VScrollTarget = nextScrollPosition.Value;

        if (MathHelper.CloseToPercent(scroll.VScroll, scroll.VScrollTarget))
            _autoScrollActive = false;
    }

    private void UpdateWarningLabel(CEPowerMonitoringFlags flags)
    {
        string key;
        Color accent;

        if ((flags & CEPowerMonitoringFlags.RoguePowerConsumer) != 0)
        {
            key = "ce-power-monitoring-window-rogue-consumer";
            accent = new Color(224, 72, 72);
        }
        else if ((flags & CEPowerMonitoringFlags.EnergyLeak) != 0)
        {
            key = "ce-power-monitoring-window-energy-leak";
            accent = new Color(224, 72, 72);
        }
        else if ((flags & CEPowerMonitoringFlags.PowerNetAbnormalities) != 0)
        {
            key = "ce-power-monitoring-window-conduit-anomalies";
            accent = new Color(224, 150, 64);
        }
        else
        {
            SystemWarningPanel.Visible = false;
            return;
        }

        SystemWarningPanel.PanelOverride = new StyleBoxFlat
        {
            BackgroundColor = new Color(0.10f, 0.10f, 0.11f, 0.92f),
            BorderColor = accent,
            BorderThickness = new Thickness(2),
        };

        SystemWarningLabel.Modulate = accent;

        var msg = new FormattedMessage();
        msg.AddMarkupOrThrow(Loc.GetString(key));
        SystemWarningLabel.SetMessage(msg);

        SystemWarningPanel.Visible = true;
    }

    private void SwitchTabsBasedOnPowerMonitoringConsoleGroup(PowerMonitoringConsoleGroup group)
    {
        MasterTabContainer.CurrentTab = group switch
        {
            PowerMonitoringConsoleGroup.Generator => 0,
            PowerMonitoringConsoleGroup.SMES => 1,
            PowerMonitoringConsoleGroup.APC => 2,
            _ => MasterTabContainer.CurrentTab,
        };
    }

    private PowerMonitoringConsoleGroup GetCurrentPowerMonitoringConsoleGroup()
    {
        return MasterTabContainer.CurrentTab switch
        {
            1 => PowerMonitoringConsoleGroup.SMES,
            2 => PowerMonitoringConsoleGroup.APC,
            _ => PowerMonitoringConsoleGroup.Generator,
        };
    }
}

public sealed class CEPowerMonitoringWindowEntry : CEPowerMonitoringWindowBaseEntry
{
    public readonly BoxContainer MainContainer;
    public readonly BoxContainer SourcesContainer;
    public readonly BoxContainer LoadsContainer;

    public CEPowerMonitoringWindowEntry(PowerMonitoringConsoleEntry entry) : base(entry)
    {
        Entry = entry;

        Orientation = LayoutOrientation.Vertical;
        HorizontalExpand = true;

        Button.StyleClasses.Add("OpenLeft");
        AddChild(Button);

        MainContainer = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            HorizontalExpand = true,
            Margin = new Thickness(8, 0, 0, 0),
            Visible = false,
        };

        AddChild(MainContainer);

        SourcesContainer = new BoxContainer { Orientation = LayoutOrientation.Vertical, HorizontalExpand = true };
        MainContainer.AddChild(SourcesContainer);

        LoadsContainer = new BoxContainer { Orientation = LayoutOrientation.Vertical, HorizontalExpand = true };
        MainContainer.AddChild(LoadsContainer);
    }
}

public sealed class CEPowerMonitoringWindowSubEntry : CEPowerMonitoringWindowBaseEntry
{
    public readonly TextureRect? Icon;

    public CEPowerMonitoringWindowSubEntry(PowerMonitoringConsoleEntry entry) : base(entry)
    {
        Orientation = LayoutOrientation.Horizontal;
        HorizontalExpand = true;

        Icon = new TextureRect
        {
            VerticalAlignment = VAlignment.Center,
            Margin = new Thickness(0, 0, 2, 0),
        };

        AddChild(Icon);

        Button.StyleClasses.Add("OpenBoth");
        AddChild(Button);
    }
}

public abstract class CEPowerMonitoringWindowBaseEntry(PowerMonitoringConsoleEntry entry) : BoxContainer
{
    public NetEntity NetEntity;
    public PowerMonitoringConsoleEntry Entry = entry;
    public readonly CEPowerMonitoringButton Button = new();
}

public sealed class CEPowerMonitoringButton : Button
{
    public readonly BoxContainer MainContainer;
    public readonly TextureRect TextureRect;
    public readonly Label NameLocalized;

    public readonly ProgressBar BatteryLevel;
    public readonly PanelContainer BackgroundPanel;
    public readonly Label BatteryPercentage;

    public readonly Label PowerValue;

    public CEPowerMonitoringButton()
    {
        HorizontalExpand = true;
        VerticalExpand = true;
        Margin = new Thickness(0f, 1f, 0f, 1f);

        MainContainer = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            SetHeight = 32f,
        };

        AddChild(MainContainer);

        TextureRect = new TextureRect
        {
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center,
            SetSize = new Vector2(32f, 32f),
            Margin = new Thickness(0f, 0f, 5f, 0f),
        };

        MainContainer.AddChild(TextureRect);

        NameLocalized = new Label { HorizontalExpand = true, ClipText = true };
        MainContainer.AddChild(NameLocalized);

        BatteryLevel = new ProgressBar
        {
            SetWidth = 47f,
            SetHeight = 20f,
            Margin = new Thickness(15, 0, 0, 0),
            MaxValue = 1,
            Visible = false,
            BackgroundStyleBoxOverride = new StyleBoxFlat { BackgroundColor = Color.Black },
        };

        MainContainer.AddChild(BatteryLevel);

        BackgroundPanel = new PanelContainer
        {
            PanelOverride = new StyleBoxFlat { BackgroundColor = new Color(0, 0, 0, 0.9f) },
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center,
            HorizontalExpand = true,
            VerticalExpand = true,
            SetSize = new Vector2(43f, 16f),
        };

        BatteryLevel.AddChild(BackgroundPanel);

        BatteryPercentage = new Label
        {
            VerticalAlignment = VAlignment.Center,
            HorizontalAlignment = HAlignment.Center,
            Align = Label.AlignMode.Center,
            SetWidth = 45f,
            MinWidth = 20f,
            Margin = new Thickness(10, -4, 10, 0),
            ClipText = true,
            Visible = false,
        };

        BackgroundPanel.AddChild(BatteryPercentage);

        PowerValue = new Label
        {
            HorizontalAlignment = HAlignment.Right,
            Align = Label.AlignMode.Right,
            SetWidth = 80f,
            Margin = new Thickness(10, 0, 0, 0),
            ClipText = true,
        };

        MainContainer.AddChild(PowerValue);
    }
}
