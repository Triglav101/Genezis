using Robust.Shared.Audio;

namespace Content.Server._Genezis.Other.CritDeathSounds;

[RegisterComponent]
public sealed partial class CritDeathSoundsComponent : Component
{
    [DataField]
    public SoundSpecifier DeathSounds = new SoundCollectionSpecifier("deathSounds");

    [DataField]
    public SoundSpecifier HeartSounds = new SoundCollectionSpecifier("heartSounds");

    [DataField]
    public bool CanOtherHearDeathSound;
}
