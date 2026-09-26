using Content.Shared.Polymorph;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.EntityEffect.Effects;

/// <summary>
/// Polymorphs the resolved entity into <see cref="PolymorphPrototype"/>. Server-side logic is
/// handled by <c>CEPolymorphEffectSystem</c>. Named with a CE prefix to avoid colliding with the
/// <c>Content.Server.Polymorph</c>/<c>Content.Shared.Polymorph</c> namespaces.
/// </summary>
public sealed partial class CEPolymorph : CEEntityEffectBase<CEPolymorph>
{
    [DataField(required: true)]
    public ProtoId<PolymorphPrototype> PolymorphPrototype;
}
