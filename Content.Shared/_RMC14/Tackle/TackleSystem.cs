using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.Administration.Logs;
using Content.Shared.CombatMode;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.Effects;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Tackle;

public sealed class TackleSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _colorFlash = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly List<EntityUid> _trackersToRemove = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<TackleableComponent, CMDisarmEvent>(OnDisarmed, before: [typeof(SharedHandsSystem), typeof(StaminaSystem)]);
    }

    private void OnDisarmed(Entity<TackleableComponent> target, ref CMDisarmEvent args)
    {
        var user = args.User;
        if (!TryComp(user, out TackleComponent? tackle))
            return;

        args.Handled = true;

        _colorFlash.RaiseEffect(Color.Aqua, new List<EntityUid> { target.Owner }, Filter.PvsExcept(user));

        var time = _timing.CurTime;

        if (_net.IsClient)
            return;

        if (TryComp(user, out CombatModeComponent? combatMode))
        {
            var audioParams = AudioParams.Default.WithVariation(0.025f).WithVolume(5f);
            _audio.PlayPredicted(combatMode.DisarmSuccessSound, target, user, audioParams);
        }
        var stun = tackle.StunMin;
        if (tackle.StunMin < tackle.StunMax)
            stun = _random.Next(tackle.StunMin, tackle.StunMax);

        stun *= 2;
        _stun.TryParalyze(target, stun, true);
    }

}
