using System.Linq;
using Content.Shared._RMC14.Dropship.Utility.Events;
using Content.Shared._RMC14.Dropship.Weapon;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Buckle.Components;
using Content.Shared.Coordinates;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Examine;
using Content.Shared.Foldable;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Medical.MedevacStretcher;

public sealed class MedevacStretcherSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedDropshipWeaponSystem _dropshipWeapon = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private static readonly EntProtoId<SkillDefinitionComponent> SkillType = "RMCSkillMedical";
    private const int MinimumRequiredSkill = 1;

    public override void Initialize()
    {
        SubscribeLocalEvent<MedevacStretcherComponent, GetVerbsEvent<InteractionVerb>>(AddActivateBeaconVerb);
        SubscribeLocalEvent<MedevacStretcherComponent, FoldedEvent>(OnFold);
        SubscribeLocalEvent<MedevacStretcherComponent, PrepareMedevacEvent>(PrepareMedevac);
        SubscribeLocalEvent<MedevacStretcherComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<MedevacStretcherComponent, InteractHandEvent>(OnInteract);
    }

    public void Medevac(Entity<MedevacStretcherComponent> ent, EntityUid medevacEntity)
    {
        if (_net.IsClient)
            return;

        if (!TryComp(ent.Owner, out StrapComponent? strap) ||
            strap.BuckledEntities.Count == 0)
        {
            return;
        }

        foreach (var buckled in strap.BuckledEntities)
        {
            _transform.PlaceNextTo(buckled, medevacEntity);
        }

        _appearance.SetData(ent, MedevacStretcherVisuals.MedevacingState, false);
    }

    private void OnExamine(Entity<MedevacStretcherComponent> ent, ref ExaminedEvent args)
    {
        if (!TryComp(ent, out StrapComponent? strap) ||
            strap.BuckledEntities.Count == 0)
        {
            return;
        }

        var name = Name(strap.BuckledEntities.First());
        using (args.PushGroup(nameof(MedevacStretcherComponent)))
        {
            args.PushText(Loc.GetString("rmc-medevac-stretcher-examine-id", ("id", name)));
        }
    }

    private void AddActivateBeaconVerb(Entity<MedevacStretcherComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        if (!_skills.HasSkill(args.User, SkillType, MinimumRequiredSkill))
            return;

        var @event = args;
        args.Verbs.Add(new InteractionVerb()
        {
            Text = Loc.GetString("rmc-medevac-toggle-beacon-verb"),
            Act = () =>
            {
                ToggleBeacon(@event.Target, @event.User);
            },
            Priority = 1,
        });
    }

    private void OnFold(Entity<MedevacStretcherComponent> ent, ref FoldedEvent args)
    {
        if (_timing.ApplyingState)
            return;
    }

    private void PrepareMedevac(Entity<MedevacStretcherComponent> ent, ref PrepareMedevacEvent args)
    {
        if (!TryComp(ent.Owner, out StrapComponent? strap) ||
            strap.BuckledEntities.Count == 0)
        {
            return;
        }

        _appearance.SetData(ent.Owner, MedevacStretcherVisuals.MedevacingState, true);
        args.ReadyForMedevac = true;
    }

    private void OnInteract(Entity<MedevacStretcherComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (TryComp(ent.Owner, out FoldableComponent? foldComp) &&
            foldComp.IsFolded)
        {
            return;
        }

        ToggleBeacon(args.Target, args.User);
        args.Handled = true;
    }

    private void ToggleBeacon(EntityUid stretcher, EntityUid user)
    {
        ActivateBeacon(stretcher, user);
    }

    private void ActivateBeacon(EntityUid stretcher, EntityUid? user)
    {

        var stretcherCoords = stretcher.ToCoordinates();
        var snappedCoords = stretcher.ToCoordinates().SnapToGrid(EntityManager, _mapManager);

        if (!HasComp<MedevacStretcherComponent>(stretcher))
            return;

        var name = GetName(stretcher);

    }

    private string GetName(Entity<StrapComponent?> stretcher)
    {
        return Resolve(stretcher, ref stretcher.Comp, false) && stretcher.Comp.BuckledEntities.Count > 0
            ? Name(stretcher.Comp.BuckledEntities.First())
            : "Empty";
    }
}

[Serializable, NetSerializable]
public enum MedevacStretcherVisuals : byte
{
    BeaconState,
    MedevacingState,
}

[Serializable, NetSerializable]
public enum BeaconVisuals : byte
{
    Off,
    On,
}
