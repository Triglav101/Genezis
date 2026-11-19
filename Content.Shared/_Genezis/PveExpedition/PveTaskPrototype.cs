using Robust.Shared.Prototypes;

namespace Content.Shared._Genezis.PveExpedition;

public enum PveTaskType : byte
{
    /// <summary>В данже спавнятся мобы Target, нужно убить их всех.</summary>
    KillMobs,

    /// <summary>В данже спавнятся предметы Target, нужно доставить их на шаттл.</summary>
    CollectItems,

    /// <summary>В данже спавнятся структуры Target, нужно уничтожить их все. Игрокам выдаётся пинпоинтер.</summary>
    DestroyStructures,

    /// <summary>Вернуть шаттл на исходную карту. Засчитывается только после остальных обязательных задач.</summary>
    ReturnShuttle,
}

/// <summary>
/// Шаблон задачи экспедиции. При старте режима из пула случайно выбираются несколько шаблонов,
/// количество целей выбирается между <see cref="MinCount"/> и <see cref="MaxCount"/>.
/// </summary>
[Prototype("pveTask")]
public sealed partial class PveTaskPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>Ключ локализации названия. Аргумент: $count.</summary>
    [DataField(required: true)]
    public LocId Name;

    /// <summary>Ключ локализации описания.</summary>
    [DataField]
    public LocId? Description;

    /// <summary>
    /// Ключ "type" зарезервирован под тип прототипа, поэтому поле называется taskType.
    /// </summary>
    [DataField("taskType", required: true)]
    public PveTaskType Type;

    /// <summary>Прототип сущности-цели (не нужен для <see cref="PveTaskType.ReturnShuttle"/>).</summary>
    [DataField]
    public EntProtoId? Target;

    [DataField]
    public int MinCount = 1;

    [DataField]
    public int MaxCount = 1;

    /// <summary>Вес при случайном выборе из пула.</summary>
    [DataField]
    public float Weight = 1f;

    /// <summary>Обязательна ли задача. Необязательные попадают только в итоги раунда.</summary>
    [DataField]
    public bool Required = true;
}
