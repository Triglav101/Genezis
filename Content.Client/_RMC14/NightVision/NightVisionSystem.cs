using Content.Shared._RMC14.NightVision;
using Content.Shared.Backmen.Eye.NightVision.Components;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client._RMC14.NightVision;

public sealed class NightVisionSystem : SharedNightVisionSystem
{
    [Dependency] private readonly ILightManager _light = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NightVisionComponent, LocalPlayerAttachedEvent>(OnNightVisionAttached);
        SubscribeLocalEvent<NightVisionComponent, LocalPlayerDetachedEvent>(OnNightVisionDetached);
    }

    private void OnNightVisionAttached(Entity<NightVisionComponent> ent, ref LocalPlayerAttachedEvent args)
    {
    }

    private void OnNightVisionDetached(Entity<NightVisionComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        Off();
    }

    private void Off()
    {
        _overlay.RemoveOverlay<NightVisionOverlay>();
        _overlay.RemoveOverlay<NightVisionFilterOverlay>();
        _light.DrawLighting = true;
    }

    private void Half(Entity<NightVisionComponent> ent)
    {
        _light.DrawLighting = true;
    }

    private void Full(Entity<NightVisionComponent> ent)
    {
        _light.DrawLighting = false;
    }
}
