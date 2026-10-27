using Content.Shared._CE.LockKey;
using Content.Shared._CE.LockKey.Components;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Containers;

namespace Content.Client._CE.LockKey.UI;

[UsedImplicitly]
public sealed partial class CELockKeyForgeBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [Dependency] private IPlayerManager _player = default!;

    private CELockKeyForgeWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<CELockKeyForgeWindow>();
        _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;
        _window.OnShapeSelected += shape => SendMessage(new CELockKeyForgeSetShapeMessage(shape));
        RefreshTemplates();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not CELockKeyForgeUiState forgeState)
            return;

        _window?.UpdateState(forgeState);
        RefreshTemplates();
    }

    /// <summary>
    /// Collects every key and portable lock the local player carries, including nested storages.
    /// </summary>
    private void RefreshTemplates()
    {
        if (_window is null || _player.LocalEntity is not { } user)
            return;

        var templates = new List<(EntityUid, string, List<int>)>();
        CollectTemplates(user, templates);
        _window.UpdateTemplates(templates);
    }

    private void CollectTemplates(EntityUid uid, List<(EntityUid, string, List<int>)> templates)
    {
        if (!EntMan.TryGetComponent<ContainerManagerComponent>(uid, out var manager))
            return;

        foreach (var container in manager.Containers.Values)
        {
            foreach (var contained in container.ContainedEntities)
            {
                if (EntMan.TryGetComponent<CEKeyComponent>(contained, out var key))
                    templates.Add((contained, EntMan.GetComponent<MetaDataComponent>(contained).EntityName, key.Shape));
                else if (EntMan.TryGetComponent<CELockComponent>(contained, out var lockComp) && lockComp.CanEmbedded)
                    templates.Add((contained, EntMan.GetComponent<MetaDataComponent>(contained).EntityName, lockComp.Shape));

                CollectTemplates(contained, templates);
            }
        }
    }
}
