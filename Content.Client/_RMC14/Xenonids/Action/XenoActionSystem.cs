using System.Linq;
using Content.Client.Actions;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Actions;
using Robust.Client.Player;
using static Content.Client.Actions.ActionsSystem;

namespace Content.Client._RMC14.Xenonids.Action;

public sealed class XenoActionSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private EntityUid? _sortedEnt;

    public override void Shutdown()
    {
        _sortedEnt = null;
    }

    public override void Update(float frameTime)
    {
        if (_player.LocalEntity is not { } player)
            return;

        if (_sortedEnt == player)
            return;

        _sortedEnt = null;

        if (!TryComp(player, out XenoComponent? xeno))
            return;

        _sortedEnt = player;

        var actions = new List<Entity<BaseActionComponent>>();
        foreach (var action in _actions.GetActions(player))
        {
            actions.Add(action);
        }

        var assignments = new List<SlotAssignment>();
        for (var i = 0; i < actions.Count; i++)
        {
            assignments.Add(new SlotAssignment(0, (byte) i, actions[i]));
        }
;
    }
}
