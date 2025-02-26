using Content.Shared.Damage;

namespace Content.Server.Damage.Components;

[RegisterComponent]
public sealed partial class DamageUserOnTriggerComponentOld : Component
{
    [DataField("ignoreResistances")] public bool IgnoreResistances;

    [DataField("damage", required: true)]
    public DamageSpecifier Damage = default!;
}
