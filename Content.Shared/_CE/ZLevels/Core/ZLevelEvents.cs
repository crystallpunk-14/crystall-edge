namespace Content.Shared._CE.ZLevels.Core.EntitySystems;

public struct ChangeViewedZLayerEvent(int oldvalue) { public int NewValue; public readonly int OldValue = oldvalue; }
