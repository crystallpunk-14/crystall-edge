using Robust.Shared.Serialization;

namespace Content.Shared._CE.Trading;

[Serializable, NetSerializable]
public enum CETradingUiKey
{
    Buy,
    Sell,
}

[Serializable, NetSerializable]
public sealed class CETradingPlatformUiState(NetEntity platform, int price) : BoundUserInterfaceState
{
    public NetEntity Platform = platform;
    public int Price = price;
}

[Serializable, NetSerializable]
public sealed class CESellingPlatformUiState(NetEntity platform, int price) : BoundUserInterfaceState
{
    public NetEntity Platform = platform;
    public int Price = price;
}

[Serializable, NetSerializable]
public readonly struct CETradingProductEntry
{
}
