using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Light.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public partial class SharedExpendableLightComponent : Component
{

    /// <summary>
    /// The icon state used by expendable lights when the they have been completely expended.
    /// </summary>
    [DataField]
    public string? IconStateSpent;

    /// <summary>
    /// The icon state used by expendable lights while they are lit.
    /// </summary>
    [DataField]
    public string? IconStateLit;

    /// <summary>
    /// The sprite layer shader used while the expendable light is lit.
    /// </summary>
    [DataField]
    public string? SpriteShaderLit = null;

    /// <summary>
    /// The sprite layer shader used after the expendable light has burnt out.
    /// </summary>
    [DataField]
    public string? SpriteShaderSpent = null;

    /// <summary>
    /// The sprite layer shader used after the expendable light has burnt out.
    /// </summary>
    [DataField]
    public Color? GlowColorLit = null;

    /// <summary>
    /// The sound that plays when the expendable light is lit.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? PlayingStream;

    /// <summary>
    ///     Status of light, whether or not it is emitting light.
    /// </summary>
    [ViewVariables]
    public bool Activated => CurrentState is ExpendableLightState.Lit or ExpendableLightState.Fading;

    [DataField, AutoNetworkedField]
    public float StateExpiryTime;

    [ViewVariables(VVAccess.ReadOnly)]
    public ExpendableLightState CurrentState { get; set; }

    [DataField("turnOnBehaviourID")]
    public string TurnOnBehaviourID { get; set; } = string.Empty;

    [DataField("fadeOutBehaviourID")]
    public string FadeOutBehaviourID { get; set; } = string.Empty;

    [DataField("glowDuration")]
    public float GlowDuration { get; set; } = 60 * 15f;

    [DataField("fadeOutDuration")]
    public float FadeOutDuration { get; set; } = 60 * 5f;

    [DataField("spentDesc")]
    public string SpentDesc { get; set; } = string.Empty;

    [DataField("spentName")]
    public string SpentName { get; set; } = string.Empty;

    [DataField("litSound")]
    public SoundSpecifier? LitSound { get; set; }

    [DataField("loopedSound")]
    public SoundSpecifier? LoopedSound { get; set; }

    [DataField("dieSound")]
    public SoundSpecifier? DieSound { get; set; } = null;
}

[Serializable, NetSerializable]
public enum ExpendableLightVisuals
{
    State,
    Behavior
}

[Serializable, NetSerializable]
public enum ExpendableLightState
{
    BrandNew,
    Lit,
    Fading,
    Dead
}

[Serializable, NetSerializable]
public enum ExpendableLightVisualLayers : byte
{
    Base = 0,
    Glow = 1,
    Overlay = 2,
}
