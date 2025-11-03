using Robust.Shared.GameStates;

namespace Content.Shared._Genezis.Penetration;

[RegisterComponent, NetworkedComponent]
public sealed partial class PenetratableComponent : Component
{
    [DataField]
    public int StoppingPower;
}
