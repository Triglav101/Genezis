using System.Numerics;
using Content.Server.NPC;
using Content.Server.NPC.Components;
using Content.Server.NPC.Pathfinding;
using Content.Server.NPC.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Genezis.PveExpedition;

/// <summary>
///     Ведёт врагов волн к ближайшему игроку и разрешает им ломать преграды (NavSmash).
///     Если у моба уже есть боевая цель (HTN сам воюет) - не вмешивается.
/// </summary>
public sealed class PveWaveHunterSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly NPCSteeringSystem _steering = default!;
    [Dependency] private readonly NPCSystem _npc = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    /// <summary>Как часто пересматривается цель одного моба.</summary>
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(3);

    /// <summary>Максимум мобов, обрабатываемых за один проход (защита от лагов при сотнях мобов).</summary>
    private const int MaxPerPass = 40;

    /// <summary>Заново прокладывать путь, только если игрок ушёл от прошлой точки дальше этого расстояния.</summary>
    private const float RepathDistance = 5f;

    private TimeSpan _nextPass;
    private readonly List<(EntityUid Uid, MapCoordinates Pos)> _players = new();

    public override void Update(float frameTime)
    {
        var now = _timing.CurTime;
        if (now < _nextPass)
            return;

        _nextPass = now + TimeSpan.FromSeconds(1);

        // Живые игроки (ghost'ы не имеют MobState).
        _players.Clear();
        var playerQuery = EntityQueryEnumerator<ActorComponent, MobStateComponent, TransformComponent>();
        while (playerQuery.MoveNext(out var uid, out _, out var state, out var xform))
        {
            if (!_mobState.IsAlive(uid, state))
                continue;

            _players.Add((uid, _transform.GetMapCoordinates(uid, xform)));
        }

        var processed = 0;
        var query = EntityQueryEnumerator<PveWaveHunterComponent, TransformComponent>();
        while (query.MoveNext(out var mob, out var hunter, out var xform))
        {
            if (hunter.NextUpdate > now)
                continue;

            if (processed++ >= MaxPerPass)
                break;

            // Небольшой разброс, чтобы мобы не пересчитывали путь все в один кадр.
            hunter.NextUpdate = now + Interval + TimeSpan.FromSeconds(_random.NextFloat(0f, 1f));

            if (_mobState.IsDead(mob))
            {
                RemCompDeferred<PveWaveHunterComponent>(mob);
                continue;
            }

            Hunt(mob, hunter, xform);
        }
    }

    private void Hunt(EntityUid mob, PveWaveHunterComponent hunter, TransformComponent xform)
    {
        // Преграды ломаем всегда (и когда идём, и когда воюем).
        if (_npc.TryGetNpc(mob, out var npc))
        {
            npc.Blackboard.SetValue(NPCBlackboard.NavSmash, true);
            npc.Blackboard.SetValue(NPCBlackboard.NavInteract, true);

            // Уже воюем с кем-то рядом - HTN сам справится.
            if (npc.Blackboard.TryGetValue<EntityUid>("Target", out var target, EntityManager) &&
                Exists(target))
            {
                return;
            }
        }

        if (xform.MapUid == null || _players.Count == 0)
            return;

        var mobPos = _transform.GetMapCoordinates(mob, xform);

        EntityUid? nearest = null;
        var nearestPos = MapCoordinates.Nullspace;
        var best = float.MaxValue;
        foreach (var (uid, pos) in _players)
        {
            if (pos.MapId != mobPos.MapId)
                continue;

            var dist = Vector2.DistanceSquared(pos.Position, mobPos.Position);
            if (dist >= best)
                continue;

            best = dist;
            nearest = uid;
            nearestPos = pos;
        }

        if (nearest == null)
            return;

        var goal = _transform.ToCoordinates(nearestPos);

        // Не дёргаем путь, если мы уже идём примерно туда же.
        if (TryComp<NPCSteeringComponent>(mob, out var steering))
        {
            steering.Flags |= PathFlags.Smashing | PathFlags.Interact;

            if (hunter.Chasing == nearest &&
                steering.Coordinates.TryDistance(EntityManager, goal, out var moved) &&
                moved < RepathDistance)
            {
                return;
            }
        }

        hunter.Chasing = nearest;
        var comp = _steering.Register(mob, goal, steering);
        comp.Flags |= PathFlags.Smashing | PathFlags.Interact;
    }
}
