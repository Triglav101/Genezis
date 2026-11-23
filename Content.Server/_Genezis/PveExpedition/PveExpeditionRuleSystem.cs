using System.Linq;
using System.Numerics;
using System.Text;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.Parallax;
using Content.Server.Pinpointer;
using Content.Server.Procedural;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared._Genezis.PveExpedition;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Maps;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Paper;
using Content.Shared.Physics;
using Content.Shared.Procedural;
using Content.Shared.Shuttles.Components;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Genezis.PveExpedition;

/// <summary>
/// Серверная логика режима «Экспедиция».
/// Игроки готовятся на шаттле без ограничений по времени, улетают на процедурную планету с огромным данжем,
/// выполняют случайные задачи (цели спавнятся только в данже), возвращают шаттл,
/// после чего идёт отсчёт до конца раунда. Пока игроки на планете, на них волнами идут враги.
/// </summary>
public sealed class PveExpeditionRuleSystem : GameRuleSystem<PveExpeditionRuleComponent>
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly BiomeSystem _biome = default!;
    [Dependency] private readonly DungeonSystem _dungeon = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly PaperSystem _paper = default!;
    [Dependency] private readonly PinpointerSystem _pinpointer = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ShuttleSystem _shuttle = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(1);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<MetaDataComponent, EntityTerminatingEvent>(OnEntityTerminating);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
        SubscribeLocalEvent<ConsoleFTLAttemptEvent>(OnConsoleFTLAttempt);
    }

    // ------------------------------------------------------------------
    //  Старт правила: выбираем случайные задачи
    // ------------------------------------------------------------------

    protected override void Started(EntityUid uid, PveExpeditionRuleComponent comp,
        GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, comp, gameRule, args);

        RollTasks(comp);

        // Если правило добавили посреди раунда - выдаём предметы уже существующим игрокам.
        var players = EntityQueryEnumerator<ActorComponent, HumanoidAppearanceComponent, MobStateComponent>();
        while (players.MoveNext(out var mob, out _, out _, out var state))
        {
            if (!_mobState.IsAlive(mob, state))
                continue;

            GiveStartItems(comp, mob);
        }

        _chat.DispatchGlobalAnnouncement(
            Loc.GetString("pve-start-announcement"),
            Loc.GetString("pve-announcement-sender"),
            true,
            colorOverride: Color.Gold);
    }

    private void RollTasks(PveExpeditionRuleComponent comp)
    {
        if (comp.TasksRolled)
            return;

        comp.TasksRolled = true;

        var pool = comp.TaskPool.Select(id => _proto.Index(id)).ToList();
        var amount = Math.Min(_random.Next(comp.MinTasks, comp.MaxTasks + 1), pool.Count);

        for (var i = 0; i < amount; i++)
        {
            var picked = PickWeighted(pool);
            pool.Remove(picked); // без повторов

            comp.Tasks.Add(new PveActiveTask
            {
                Proto = picked,
                Count = _random.Next(picked.MinCount, picked.MaxCount + 1),
            });
        }

        // Возврат шаттла - всегда последней обязательной задачей.
        var returnProto = _proto.Index(comp.ReturnTask);
        comp.Tasks.Add(new PveActiveTask
        {
            Proto = returnProto,
            Count = 1,
        });
    }

    private PveTaskPrototype PickWeighted(List<PveTaskPrototype> pool)
    {
        var total = pool.Sum(p => p.Weight);
        var roll = _random.NextFloat() * total;

        foreach (var p in pool)
        {
            roll -= p.Weight;
            if (roll <= 0)
                return p;
        }

        return pool[^1];
    }

    // ------------------------------------------------------------------
    //  Основной цикл
    // ------------------------------------------------------------------

    protected override void ActiveTick(EntityUid uid, PveExpeditionRuleComponent comp,
        GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, comp, gameRule, frameTime);

        var now = _timing.CurTime;

        switch (comp.Phase)
        {
            case PveExpeditionPhase.Preparation:
                if (!comp.Initialized && !TryInit(comp))
                    return; // станция ещё не создана - попробуем в следующий тик

                PollDungeon(comp);

                if (comp.PlanetReady && HasLanded(comp))
                {
                    comp.Phase = PveExpeditionPhase.Landing;
                    comp.LandedAt = now;
                }

                break;

            case PveExpeditionPhase.Landing:
                if (now < comp.LandedAt + comp.LandingDelay)
                    return;

                comp.Phase = PveExpeditionPhase.Active;
                comp.NextHostileSpawn = now + comp.HostileStartInterval;
                comp.NextCheck = now;

                _chat.DispatchGlobalAnnouncement(
                    Loc.GetString("pve-landed-announcement") + "\n" + BuildTaskList(comp),
                    Loc.GetString("pve-announcement-sender"),
                    true,
                    colorOverride: Color.Gold);
                break;

            case PveExpeditionPhase.Active:
                TickHostiles(comp, now);

                if (now < comp.NextCheck)
                    return;

                comp.NextCheck = now + CheckInterval;

                UpdateCollectTasks(comp);
                UpdateReturnTask(comp);
                UpdatePinpointers(comp);

                if (comp.Tasks.Where(t => t.Proto.Required).All(t => t.Completed))
                {
                    comp.Phase = PveExpeditionPhase.Countdown;
                    comp.Victory = true;
                    comp.CountdownEnd = now + comp.EndCountdown;

                    _chat.DispatchGlobalAnnouncement(
                        Loc.GetString("pve-countdown-start",
                            ("seconds", (int) comp.EndCountdown.TotalSeconds)),
                        Loc.GetString("pve-announcement-sender"),
                        true,
                        colorOverride: Color.LightGreen);
                }

                break;

            case PveExpeditionPhase.Countdown:
                var left = comp.CountdownEnd - now;

                if (!comp.Warned60 && left <= TimeSpan.FromSeconds(60))
                {
                    comp.Warned60 = true;
                    _chat.DispatchGlobalAnnouncement(Loc.GetString("pve-countdown-60"),
                        Loc.GetString("pve-announcement-sender"), true, colorOverride: Color.Orange);
                }

                if (!comp.Warned10 && left <= TimeSpan.FromSeconds(10))
                {
                    comp.Warned10 = true;
                    _chat.DispatchGlobalAnnouncement(Loc.GetString("pve-countdown-10"),
                        Loc.GetString("pve-announcement-sender"), true, colorOverride: Color.Red);
                }

                if (left <= TimeSpan.Zero)
                {
                    comp.Phase = PveExpeditionPhase.Ended;
                    GameTicker.EndRound(Loc.GetString("pve-round-end-text"));
                }

                break;
        }
    }

    // ------------------------------------------------------------------
    //  Инициализация: планета + данж (в фоне, пока экипаж готовится)
    // ------------------------------------------------------------------

    private bool TryInit(PveExpeditionRuleComponent comp)
    {
        // Станция создаётся из gameMap (см. YAML): её самая большая грид - стартовый шаттл.
        var stations = EntityQueryEnumerator<StationDataComponent>();
        while (stations.MoveNext(out _, out var data))
        {
            var grid = _station.GetLargestGrid(data);
            if (grid == null)
                continue;

            comp.ShuttleGrid = grid;
            EnsureComp<ShuttleComponent>(grid.Value);

            var startMap = Transform(grid.Value).MapUid;
            comp.StartMap = startMap;

            // Домой можно вернуться в любую точку стартовой карты.
            if (startMap != null && TryComp<MapComponent>(startMap, out var startMapComp))
                _shuttle.TryAddFTLDestination(startMapComp.MapId, true, false, false, out _);

            comp.Initialized = true;
            CreatePlanet(comp);
            return true;
        }

        return false;
    }

    private void CreatePlanet(PveExpeditionRuleComponent comp)
    {
        var mapUid = _map.CreateMap(out _, runMapInit: false);
        comp.PlanetMap = mapUid;

        var biomeId = _random.Pick(comp.Planets);
        _biome.EnsurePlanet(mapUid, _proto.Index(biomeId), _random.Next());
        _meta.SetEntityName(mapUid, Loc.GetString("pve-planet-name"));

        // Постоянные газы планеты (например, Mirage): MapAtmosphere бесконечна, как и обычный воздух планеты.
        foreach (var (gas, moles) in comp.PlanetGases)
        {
            _atmos.AddMapGas(mapUid, gas, moles);
        }

        StartDungeon(comp);
    }

    private void StartDungeon(PveExpeditionRuleComponent comp)
    {
        if (comp.PlanetMap is not { } mapUid || !TryComp<MapGridComponent>(mapUid, out var grid))
            return;

        comp.DungeonAttempts++;

        var config = _proto.Index(_random.Pick(comp.DungeonConfigs));

        // Данж центрируем на случайной точке вдали от посадочного маяка (маяк стоит в начале координат).
        var distance = _random.NextFloat(comp.MinDungeonDistance, comp.MaxDungeonDistance);
        var center = _random.NextAngle().ToVec() * distance;
        var corner = center - comp.DungeonSize / 2f;
        var position = new Vector2i((int) MathF.Floor(corner.X), (int) MathF.Floor(corner.Y));

        comp.DungeonTask = _dungeon.GenerateDungeonAsync(config, mapUid, grid, position, _random.Next());
    }

    private void PollDungeon(PveExpeditionRuleComponent comp)
    {
        var task = comp.DungeonTask;
        if (task == null || !task.IsCompleted)
            return;

        comp.DungeonTask = null;

        if (task.IsCompletedSuccessfully)
        {
#pragma warning disable RA0004 // Risk of deadlock from accessing Task<T>.Result
            var rooms = task.Result.SelectMany(d => d.Rooms).ToList();
#pragma warning restore RA0004 // Risk of deadlock from accessing Task<T>.Result
            if (rooms.Count > 0)
            {
                OnDungeonReady(comp, rooms);
                return;
            }

            Log.Error("PvE expedition: dungeon generated zero rooms.");
        }
        else
        {
            Log.Error($"PvE expedition: dungeon generation failed: {task.Exception}");
        }

        if (comp.DungeonAttempts < comp.MaxDungeonAttempts)
        {
            StartDungeon(comp);
            return;
        }

        // Совсем не вышло - честно заканчиваем раунд, а не оставляем экипаж навсегда на шаттле.
        comp.Phase = PveExpeditionPhase.Ended;
        GameTicker.EndRound(Loc.GetString("pve-generation-failed"));
    }

    private void OnDungeonReady(PveExpeditionRuleComponent comp, List<DungeonRoom> rooms)
    {
        if (comp.PlanetMap is not { } mapUid || comp.ShuttleGrid is not { } shuttle)
            return;

        SpawnTaskTargets(comp, mapUid, rooms);

        // Маяк посадки - рядом с текущей позицией шаттла, чтобы прыжок укладывался в FTL-дальность.
        var beaconPos = _transform.GetWorldPosition(shuttle);
        var beacon = Spawn("FTLPoint", new EntityCoordinates(mapUid, beaconPos));
        _meta.SetEntityName(beacon, Loc.GetString(comp.BeaconName));
        comp.Beacon = beacon;

        // На планету можно только по маяку.
        if (TryComp<MapComponent>(mapUid, out var mapComp))
            _shuttle.TryAddFTLDestination(mapComp.MapId, true, false, true, out _);

        _map.InitializeMap(mapUid);

        comp.PlanetReady = true;

        _chat.DispatchGlobalAnnouncement(
            Loc.GetString("pve-planet-ready-announcement", ("rooms", rooms.Count)),
            Loc.GetString("pve-announcement-sender"),
            true,
            colorOverride: Color.Gold);
    }

    private bool HasLanded(PveExpeditionRuleComponent comp)
    {
        if (comp.ShuttleGrid is not { } grid || comp.PlanetMap is not { } map)
            return false;

        return Transform(grid).MapUid == map && !HasComp<FTLComponent>(grid);
    }

    // ------------------------------------------------------------------
    //  Спавн целей задач строго внутри комнат данжа
    // ------------------------------------------------------------------

    private void SpawnTaskTargets(PveExpeditionRuleComponent comp, EntityUid mapUid, List<DungeonRoom> rooms)
    {
        if (!TryComp<MapGridComponent>(mapUid, out var grid))
            return;

        var unusedRooms = new List<DungeonRoom>(rooms);

        foreach (var task in comp.Tasks)
        {
            if (task.Proto.Type == PveTaskType.ReturnShuttle)
                continue;

            if (task.Proto.Target is not { } target)
            {
                Log.Error($"PvE expedition: task {task.Proto.ID} has no target prototype, auto-completing it.");
                task.Completed = true;
                continue;
            }

            for (var i = 0; i < task.Count; i++)
            {
                // Пока есть комнаты без целей - раскладываем по разным комнатам, чтобы данж пришлось исследовать.
                if (unusedRooms.Count == 0)
                    unusedRooms.AddRange(rooms);

                var room = _random.PickAndTake(unusedRooms);
                var coords = PickRoomCoordinates(mapUid, grid, room);
                var ent = Spawn(target, coords);

                PrepareTarget(ent, task.Proto.Type);
                task.Tracked.Add(ent);
            }
        }
    }

    private EntityCoordinates PickRoomCoordinates(EntityUid mapUid, MapGridComponent grid, DungeonRoom room)
    {
        for (var attempt = 0; attempt < 30 && room.Tiles.Count > 0; attempt++)
        {
            var tile = room.Tiles.ElementAt(_random.Next(room.Tiles.Count));

            if (_turf.IsTileBlocked(mapUid, tile, CollisionGroup.MobMask, grid))
                continue;

            return _map.GridTileToLocal(mapUid, grid, tile);
        }

        // Все попытки упёрлись в мебель - ставим в центр комнаты.
        var center = new Vector2i((int) MathF.Floor(room.Center.X), (int) MathF.Floor(room.Center.Y));
        return _map.GridTileToLocal(mapUid, grid, center);
    }

    private void PrepareTarget(EntityUid ent, PveTaskType type)
    {
        if (type != PveTaskType.DestroyStructures)
            return;

        // Метка для пинпоинтеров.
        EnsureComp<PveTaskTargetComponent>(ent);

        // Закрепление здесь намеренно не делается: цели-структуры (генераторы, СМЭС) спавнятся
        // закреплёнными прямо из прототипа (transform.anchored), а повторный AnchorEntity
        // роняет DebugTools.Assert в MapChunk.AddToSnapGridCell.
    }

    // ------------------------------------------------------------------
    //  Враги: чем дольше экспедиция, тем чаще волны и шире список групп
    // ------------------------------------------------------------------

    private void TickHostiles(PveExpeditionRuleComponent comp, TimeSpan now)
    {
        if (now < comp.NextHostileSpawn)
            return;

        var elapsed = now - comp.LandedAt;
        var ramp = comp.HostileRampTime.TotalSeconds <= 0
            ? 1.0
            : Math.Clamp(elapsed.TotalSeconds / comp.HostileRampTime.TotalSeconds, 0.0, 1.0);

        var interval = comp.HostileStartInterval.TotalSeconds
                       + (comp.HostileMinInterval.TotalSeconds - comp.HostileStartInterval.TotalSeconds) * ramp;

        comp.NextHostileSpawn = now + TimeSpan.FromSeconds(Math.Max(1.0, interval));

        SpawnHostileGroup(comp, elapsed);
    }

    private void SpawnHostileGroup(PveExpeditionRuleComponent comp, TimeSpan elapsed)
    {
        if (comp.PlanetMap is not { } mapUid || !TryComp<MapGridComponent>(mapUid, out var grid))
            return;

        comp.Hostiles.RemoveWhere(e => Deleted(e) || _mobState.IsDead(e));

        var free = comp.MaxHostiles - comp.Hostiles.Count;
        if (free <= 0)
            return;

        // Группы, которым пора "открыться" по времени.
        var unlocked = comp.HostileGroups
            .Select(id => _proto.Index(id))
            .Where(g => elapsed >= g.UnlockAfter)
            .ToList();

        if (unlocked.Count == 0)
            return;

        var players = GetPlayersOnPlanet(mapUid);
        if (players.Count == 0)
            return;

        var group = PickWeightedGroup(unlocked);
        var count = Math.Min(_random.Next(group.MinCount, group.MaxCount + 1), free);

        var anchor = FindHostileAnchor(comp, mapUid, grid, _random.Pick(players));
        if (anchor == null)
            return;

        for (var i = 0; i < count; i++)
        {
            var coords = ScatterAround(mapUid, grid, anchor.Value);
            var mob = Spawn(group.Entity, coords);
            comp.Hostiles.Add(mob);

            // Враги волн всегда знают, где ближайший игрок, идут к нему и ломают преграды.
            // Цели задач спавнятся в другом месте и этот компонент не получают.
            EnsureComp<PveWaveHunterComponent>(mob);
        }
    }

    private PveHostileGroupPrototype PickWeightedGroup(List<PveHostileGroupPrototype> groups)
    {
        var total = groups.Sum(g => g.Weight);
        var roll = _random.NextFloat() * total;

        foreach (var g in groups)
        {
            roll -= g.Weight;
            if (roll <= 0)
                return g;
        }

        return groups[^1];
    }

    private List<EntityUid> GetPlayersOnPlanet(EntityUid mapUid)
    {
        var result = new List<EntityUid>();
        var query = EntityQueryEnumerator<ActorComponent, MobStateComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var state, out var xform))
        {
            if (xform.MapUid != mapUid || !_mobState.IsAlive(uid, state))
                continue;

            result.Add(uid);
        }

        return result;
    }

    /// <summary>Находит свободную прогруженную клетку на заданном расстоянии от игрока.</summary>
    private Vector2? FindHostileAnchor(PveExpeditionRuleComponent comp, EntityUid mapUid, MapGridComponent grid, EntityUid player)
    {
        var origin = _transform.GetWorldPosition(player);

        for (var attempt = 0; attempt < 20; attempt++)
        {
            var distance = _random.NextFloat(comp.HostileMinDistance, comp.HostileMaxDistance);
            var pos = origin + _random.NextAngle().ToVec() * distance;

            if (IsFreeTile(mapUid, grid, pos))
                return pos;
        }

        return null;
    }

    private EntityCoordinates ScatterAround(EntityUid mapUid, MapGridComponent grid, Vector2 anchor)
    {
        for (var attempt = 0; attempt < 6; attempt++)
        {
            var pos = anchor + _random.NextAngle().ToVec() * _random.NextFloat(0f, 3f);
            if (IsFreeTile(mapUid, grid, pos))
                return new EntityCoordinates(mapUid, pos);
        }

        return new EntityCoordinates(mapUid, anchor);
    }

    private bool IsFreeTile(EntityUid mapUid, MapGridComponent grid, Vector2 worldPos)
    {
        if (!_map.TryGetTileRef(mapUid, grid, worldPos, out var tile) || tile.Tile.IsEmpty)
            return false;

        return !_turf.IsTileBlocked(tile, CollisionGroup.MobMask);
    }

    // ------------------------------------------------------------------
    //  Отслеживание выполнения задач
    // ------------------------------------------------------------------

    private void OnMobStateChanged(MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
            OnTargetGone(args.Target, PveTaskType.KillMobs);
    }

    private void OnEntityTerminating(Entity<MetaDataComponent> ent, ref EntityTerminatingEvent args)
    {
        // Убитый и затем гибнутый моб уже засчитан; удалённая структура засчитывается здесь.
        OnTargetGone(ent.Owner, PveTaskType.KillMobs);
        OnTargetGone(ent.Owner, PveTaskType.DestroyStructures);
    }

    private void OnTargetGone(EntityUid target, PveTaskType type)
    {
        var query = EntityQueryEnumerator<PveExpeditionRuleComponent>();
        while (query.MoveNext(out var comp))
        {
            if (comp.Phase != PveExpeditionPhase.Active)
                continue;

            foreach (var task in comp.Tasks)
            {
                if (task.Completed || task.Proto.Type != type || !task.Tracked.Remove(target))
                    continue;

                task.Progress++;

                if (task.Tracked.Count == 0)
                    CompleteTask(task);
            }
        }
    }

    private void UpdateCollectTasks(PveExpeditionRuleComponent comp)
    {
        if (comp.ShuttleGrid is not { } shuttle)
            return;

        foreach (var task in comp.Tasks)
        {
            if (task.Completed || task.Proto.Type != PveTaskType.CollectItems)
                continue;

            // Уничтоженные предметы не требуем, иначе задача станет невыполнимой.
            task.Tracked.RemoveWhere(e => Deleted(e));

            var onShuttle = task.Tracked.Count(e => Transform(e).GridUid == shuttle);
            task.Progress = onShuttle;

            if (onShuttle >= task.Tracked.Count)
                CompleteTask(task);
        }
    }

    /// <summary>Возврат шаттла засчитывается, только когда все остальные обязательные задачи уже выполнены.</summary>
    private void UpdateReturnTask(PveExpeditionRuleComponent comp)
    {
        if (comp.ShuttleGrid is not { } shuttle || comp.StartMap is not { } startMap)
            return;

        foreach (var task in comp.Tasks)
        {
            if (task.Completed || task.Proto.Type != PveTaskType.ReturnShuttle)
                continue;

            if (!OtherRequiredTasksDone(comp))
                continue;

            if (Transform(shuttle).MapUid == startMap && !HasComp<FTLComponent>(shuttle))
            {
                task.Progress = 1;
                CompleteTask(task);
            }
        }
    }

    private static bool OtherRequiredTasksDone(PveExpeditionRuleComponent comp)
    {
        return comp.Tasks
            .Where(t => t.Proto.Required && t.Proto.Type != PveTaskType.ReturnShuttle)
            .All(t => t.Completed);
    }

    private void CompleteTask(PveActiveTask task)
    {
        task.Completed = true;

        _chat.DispatchGlobalAnnouncement(
            Loc.GetString("pve-task-completed", ("task", Loc.GetString(task.Proto.Name, ("count", task.Count)))),
            Loc.GetString("pve-announcement-sender"),
            true,
            colorOverride: Color.LightGreen);
    }

    /// <summary>
    /// Нельзя улететь с планеты, пока не выполнены все обязательные задачи (кроме самого возврата).
    /// </summary>
    private void OnConsoleFTLAttempt(ref ConsoleFTLAttemptEvent ev)
    {
        var query = EntityQueryEnumerator<PveExpeditionRuleComponent>();
        while (query.MoveNext(out var comp))
        {
            if (comp.ShuttleGrid != ev.Uid
                || comp.Phase is not (PveExpeditionPhase.Landing or PveExpeditionPhase.Active)
                || Transform(ev.Uid).MapUid != comp.PlanetMap)
            {
                continue;
            }

            if (OtherRequiredTasksDone(comp))
                continue;

            ev.Cancelled = true;
            ev.Reason = Loc.GetString("pve-ftl-blocked");
            return;
        }
    }

    // ------------------------------------------------------------------
    //  Предметы игрокам: бумага с задачами и пинпоинтер
    // ------------------------------------------------------------------

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        var query = EntityQueryEnumerator<PveExpeditionRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var comp, out var gameRule))
        {
            if (!GameTicker.IsGameRuleActive(uid, gameRule))
                continue;

            // Задачи выбираются при старте правила, поэтому игрок получает полный список сразу, ещё до вылета.
            RollTasks(comp);
            GiveStartItems(comp, args.Mob);
        }
    }

    private void GiveStartItems(PveExpeditionRuleComponent comp, EntityUid mob)
    {
        var coords = Transform(mob).Coordinates;

        var paper = Spawn(comp.PaperProto, coords);
        if (TryComp<PaperComponent>(paper, out var paperComp))
        {
            _meta.SetEntityName(paper, Loc.GetString("pve-paper-name"));
            _paper.SetContent((paper, paperComp), BuildPaperText(comp));
        }

        GiveItem(mob, paper);

        // Задача на уничтожение структур -> пинпоинтер в рюкзак.
        if (comp.Tasks.Any(t => t.Proto.Type == PveTaskType.DestroyStructures))
        {
            var pinpointer = Spawn(comp.PinpointerProto, coords);
            comp.Pinpointers.Add(pinpointer);
            GiveItem(mob, pinpointer);
        }
    }

    private void GiveItem(EntityUid mob, EntityUid item)
    {
        if (_inventory.TryGetSlotEntity(mob, "back", out var back)
            && TryComp<StorageComponent>(back, out var storage)
            && _storage.Insert(back.Value, item, out _, user: null, storageComp: storage, playSound: false))
        {
            return;
        }

        // Нет рюкзака или он полон - в руки/на пол.
        _hands.PickupOrDrop(mob, item);
    }

    private string BuildPaperText(PveExpeditionRuleComponent comp)
    {
        var sb = new StringBuilder();
        sb.AppendLine(Loc.GetString("pve-paper-header"));
        sb.AppendLine();

        var index = 1;
        foreach (var task in comp.Tasks)
        {
            sb.Append(index++).Append(". [bold]")
                .Append(Loc.GetString(task.Proto.Name, ("count", task.Count)))
                .Append("[/bold]");

            if (!task.Proto.Required)
                sb.Append(' ').Append(Loc.GetString("pve-task-optional"));

            if (task.Proto.Description is { } desc)
                sb.AppendLine().Append("   ").Append(Loc.GetString(desc, ("count", task.Count)));

            sb.AppendLine();
        }

        sb.AppendLine();
        sb.AppendLine(Loc.GetString("pve-paper-footer"));
        return sb.ToString();
    }

    private string BuildTaskList(PveExpeditionRuleComponent comp)
    {
        var lines = comp.Tasks.Select(t =>
            "- " + Loc.GetString(t.Proto.Name, ("count", t.Count))
            + (t.Proto.Required ? "" : " " + Loc.GetString("pve-task-optional")));

        return string.Join("\n", lines);
    }

    /// <summary>Каждую секунду наводим пинпоинтеры на ближайшую оставшуюся цель-структуру.</summary>
    private void UpdatePinpointers(PveExpeditionRuleComponent comp)
    {
        if (comp.Pinpointers.Count == 0)
            return;

        comp.Pinpointers.RemoveWhere(p => Deleted(p));

        var targets = new List<EntityUid>();
        foreach (var task in comp.Tasks)
        {
            if (task.Completed || task.Proto.Type != PveTaskType.DestroyStructures)
                continue;

            targets.AddRange(task.Tracked.Where(e => !Deleted(e)));
        }

        foreach (var pinpointer in comp.Pinpointers)
        {
            var xform = Transform(pinpointer);
            var pos = _transform.GetWorldPosition(xform);

            EntityUid? best = null;
            var bestDist = float.MaxValue;

            foreach (var target in targets)
            {
                var targetXform = Transform(target);
                if (targetXform.MapID != xform.MapID)
                    continue;

                var dist = (_transform.GetWorldPosition(targetXform) - pos).LengthSquared();
                if (dist >= bestDist)
                    continue;

                bestDist = dist;
                best = target;
            }

            _pinpointer.SetTarget(pinpointer, best);

            if (best != null && TryComp<Content.Shared.Pinpointer.PinpointerComponent>(pinpointer, out var pin) && !pin.IsActive)
                _pinpointer.TogglePinpointer(pinpointer, pin);
        }
    }

    // ------------------------------------------------------------------
    //  Итоги раунда
    // ------------------------------------------------------------------

    protected override void AppendRoundEndText(EntityUid uid, PveExpeditionRuleComponent comp,
        GameRuleComponent gameRule, ref RoundEndTextAppendEvent args)
    {
        args.AddLine(Loc.GetString(comp.Victory ? "pve-roundend-success" : "pve-roundend-incomplete"));

        foreach (var task in comp.Tasks)
        {
            args.AddLine(Loc.GetString(task.Completed ? "pve-roundend-task-done" : "pve-roundend-task-failed",
                ("name", Loc.GetString(task.Proto.Name, ("count", task.Count)))));
        }
    }
}
