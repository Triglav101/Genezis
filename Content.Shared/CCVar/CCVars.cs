using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

/// <summary>
/// Contains all the CVars used by content.
/// </summary>
/// <remarks>
/// NOTICE FOR FORKS: Put your own CVars in a separate file with a different [CVarDefs] attribute. RT will automatically pick up on it.
/// </remarks>
[CVarDefs]
public sealed partial class CCVars : CVars
{
    /// <summary>
    /// Impulse multiplier for player interactions that move grids (other than shuttle thrusters, gyroscopes and grid collisons).
    /// At the moment this affects the pushback in SpraySystem and from shooting guns.
    /// A higher value means grids have a lower effective mass and therefore will get pushed stronger.
    /// A value of 0 will disable pushback.
    /// The default has been chosen such that a one tile grid roughly equals 2/3 Urist masses.
    /// TODO: Make grid mass a sane number so we can get rid of this.
    /// At the moment they have a very low mass of roughly 0.48 kg per tile independent of any walls or anchored objects on them.
    /// </summary>
    public static readonly CVarDef<float> GridImpulseMultiplier =
        CVarDef.Create("shuttle.grid_impulse_multiplier", 0.01f, CVar.REPLICATED | CVar.SERVER);

    /// <summary>
    /// A simple toggle to test <c>OptionsVisualizerComponent</c>.
    /// </summary>
    public static readonly CVarDef<bool> DebugOptionVisualizerTest =
        CVarDef.Create("debug.option_visualizer_test", false, CVar.CLIENTONLY);

    /// <summary>
    /// Set to true to disable parallel processing in the pow3r solver.
    /// </summary>
    public static readonly CVarDef<bool> DebugPow3rDisableParallel =
        CVarDef.Create("debug.pow3r_disable_parallel", true, CVar.SERVERONLY);
}
