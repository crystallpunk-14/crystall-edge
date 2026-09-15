namespace Content.Client.Graphics;

public static class ContentPostShaderIds
{
    public const string Stealth = "stealth";
    public const string FloorOcclusion = "floor-occlusion";
    public const string Holopad = "holopad";
    public const string InteractionOutline = "interaction-outline";
    public const string TargetOutline = "target-outline";
    public const string DragDropOutline = "drag-drop-outline";
    // CrystallEdge zone
    public const string CEMurkDissolving = "ce-murk-dissolving";
    // CrystallEdge end

    public static readonly string[] BeforeOutlines =
    {
        InteractionOutline,
        TargetOutline,
        DragDropOutline,
    };

    public static readonly string[] AfterBaseEffects =
    {
        Stealth,
        FloorOcclusion,
        Holopad,
        // CrystallEdge zone
        CEMurkDissolving,
        // CrystallEdge end
    };
}
