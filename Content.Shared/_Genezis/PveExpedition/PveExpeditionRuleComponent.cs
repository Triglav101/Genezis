using System.Numerics;
using System.Threading.Tasks;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Procedural;
using Robust.Shared.Prototypes;

namespace Content.Shared._Genezis.PveExpedition;

public enum PveExpeditionPhase : byte
{
    /// <summary>Экипаж на шаттле, готовится без ограничения по времени. Планета с данжем генерируется в фоне.</summary>
    Preparation,

    /// <summary>Шаттл прибыл на планету; короткая пауза перед стартом экспедиции.</summary>
    Landing,

    /// <summary>Идёт выполнение задач в данже, спавнятся враги.</summary>
    Active,

    /// <summary>Все обязательные задачи выполнены (шаттл вернулся), идёт отсчёт до конца раунда.</summary>
    Countdown,

    Ended,
}

/// <summary>Рантайм-состояние одной задачи этого раунда.</summary>
public sealed class PveActiveTask
{
    public PveTaskPrototype Proto = default!;

    /// <summary>Сколько целей нужно (выбрано случайно в диапазоне прототипа).</summary>
    public int Count;

    /// <summary>Сколько целей уже выполнено.</summary>
    public int Progress;

    public bool Completed;

    /// <summary>Цели, которые ещё не выполнены (мобы, предметы, структуры).</summary>
    public readonly HashSet<EntityUid> Tracked = new();
}

/// <summary>
/// GameRule PvE-режима «Экспедиция»:
/// шаттл -> процедурная планета с огромным данжем -> случайные задачи в данже ->
/// возврат шаттла -> отсчёт -> конец раунда.
/// </summary>
[RegisterComponent]
public sealed partial class PveExpeditionRuleComponent : Component
{
    // ---------------- Планета и данж ----------------

    /// <summary>Пул биомов; планета выбирается случайно.</summary>
    [DataField(required: true)]
    public List<ProtoId<BiomeTemplatePrototype>> Planets = new();

    /// <summary>Пул конфигов данжа; выбирается случайно. Должны давать МНОГО комнат.</summary>
    [DataField(required: true)]
    public List<ProtoId<DungeonConfigPrototype>> DungeonConfigs = new();

    /// <summary>Минимальная дистанция от посадочного маяка до центра данжа (в тайлах).</summary>
    [DataField]
    public float MinDungeonDistance = 140f;

    /// <summary>Максимальная дистанция от посадочного маяка до центра данжа (в тайлах).</summary>
    [DataField]
    public float MaxDungeonDistance = 170f;

    /// <summary>Примерный размер данжа в тайлах. Нужен, чтобы центрировать данж вокруг выбранной точки.</summary>
    [DataField]
    public Vector2 DungeonSize = new(98f, 78f);

    /// <summary>Сколько раз пробуем сгенерировать данж, если задача упала.</summary>
    [DataField]
    public int MaxDungeonAttempts = 3;

    /// <summary>Название посадочного маяка в консоли шаттла.</summary>
    [DataField]
    public LocId BeaconName = "pve-destination-name";

    // ---------------- Задачи ----------------

    /// <summary>Пул случайных задач.</summary>
    [DataField(required: true)]
    public List<ProtoId<PveTaskPrototype>> TaskPool = new();

    /// <summary>Задача возврата шаттла; добавляется всегда последней.</summary>
    [DataField(required: true)]
    public ProtoId<PveTaskPrototype> ReturnTask;

    [DataField]
    public int MinTasks = 3;

    [DataField]
    public int MaxTasks = 5;

    /// <summary>Отсчёт до конца раунда после выполнения всех обязательных задач.</summary>
    [DataField]
    public TimeSpan EndCountdown = TimeSpan.FromMinutes(2);

    /// <summary>Пауза после посадки перед началом спавна врагов.</summary>
    [DataField]
    public TimeSpan LandingDelay = TimeSpan.FromSeconds(10);

    // ---------------- Предметы игрокам ----------------

    [DataField]
    public EntProtoId PinpointerProto = "PinpointerPveStructures";

    [DataField]
    public EntProtoId PaperProto = "Paper";

    // ---------------- Враги ----------------

    /// <summary>Список групп врагов (с временем разблокировки каждой).</summary>
    [DataField(required: true)]
    public List<ProtoId<PveHostileGroupPrototype>> HostileGroups = new();

    /// <summary>Интервал между спавнами сразу после посадки.</summary>
    [DataField]
    public TimeSpan HostileStartInterval = TimeSpan.FromSeconds(120);

    /// <summary>Минимальный интервал между спавнами (достигается к концу <see cref="HostileRampTime"/>).</summary>
    [DataField]
    public TimeSpan HostileMinInterval = TimeSpan.FromSeconds(15);

    /// <summary>За какое время после посадки интервал линейно падает от стартового до минимального.</summary>
    [DataField]
    public TimeSpan HostileRampTime = TimeSpan.FromMinutes(40);

    /// <summary>Максимум одновременно живых заспавненных врагов.</summary>
    [DataField]
    public int MaxHostiles = 70;

    /// <summary>Минимальная дистанция спавна врагов от случайного игрока на планете.</summary>
    [DataField]
    public float HostileMinDistance = 28f;

    /// <summary>Максимальная дистанция спавна врагов от случайного игрока на планете.</summary>
    [DataField]
    public float HostileMaxDistance = 55f;

    // ---------------- Рантайм ----------------

    [ViewVariables] public PveExpeditionPhase Phase = PveExpeditionPhase.Preparation;
    [ViewVariables] public bool TasksRolled;
    [ViewVariables] public bool Initialized;
    [ViewVariables] public bool PlanetReady;
    [ViewVariables] public int DungeonAttempts;
    [ViewVariables] public EntityUid? ShuttleGrid;
    [ViewVariables] public EntityUid? StartMap;
    [ViewVariables] public EntityUid? PlanetMap;
    [ViewVariables] public EntityUid? Beacon;
    [ViewVariables] public TimeSpan LandedAt;
    [ViewVariables] public TimeSpan CountdownEnd;
    [ViewVariables] public TimeSpan NextCheck;
    [ViewVariables] public TimeSpan NextHostileSpawn;
    [ViewVariables] public bool Warned60;
    [ViewVariables] public bool Warned10;
    [ViewVariables] public bool Victory;

    /// <summary>Фоновая задача генерации данжа.</summary>
    public Task<List<Dungeon>>? DungeonTask;

    public readonly List<PveActiveTask> Tasks = new();

    /// <summary>Выданные игрокам пинпоинтеры - им периодически выставляем цель.</summary>
    public readonly HashSet<EntityUid> Pinpointers = new();

    /// <summary>Живые враги, заспавненные волнами (для лимита).</summary>
    public readonly HashSet<EntityUid> Hostiles = new();
}
