using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.ResourceManager;

[ImplicitDataDefinitionForInheritors]
[MeansImplicitUse]
public abstract partial class CEResourceRequirement
{
    /// <summary>
    /// Here a check is made that the recipe as a whole can be fulfilled at the current moment. Do not add anything that affects gameplay here, and only perform checks here.
    /// </summary>
    /// <returns></returns>
    public abstract bool CheckRequirement(IEntityManager entManager,
        IPrototypeManager protoManager,
        HashSet<EntityUid> placedEntities);

    /// <summary>
    /// An event that is triggered after crafting. This is the place to put important things like removing items, spending stacks or other things.
    /// </summary>
    public virtual void PostCraft(IEntityManager entManager,
        IPrototypeManager protoManager,
        HashSet<EntityUid> placedEntities)
    {

    }

    public virtual double GetPrice(IEntityManager entManager,
        IPrototypeManager protoManager)
    {
        return 0;
    }

    /// <summary>
    /// This text will be displayed in the description of the craft recipe. Write something like ‘Wooden planks: х10’ here
    /// </summary>
    public virtual string GetRequirementTitle(IPrototypeManager protoManager)
    {
        return string.Empty;
    }

    /// <summary>
    /// Short amount label drawn next to the requirement icon, e.g. "10". Empty to hide.
    /// </summary>
    public virtual string GetRequirementAmount()
    {
        return string.Empty;
    }

    /// <summary>
    /// Icon layers drawn bottom to top. Empty to hide the icon.
    /// </summary>
    public virtual List<CEResourceIconLayer> GetRequirementIcon(IEntityManager entManager, IPrototypeManager protoManager)
    {
        return new List<CEResourceIconLayer>();
    }
}
