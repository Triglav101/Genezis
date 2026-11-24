using Robust.Shared.Prototypes;

namespace Content.Shared._Genezis.PveExpedition;

/// <summary>
/// Запись в списке враждебных групп, которые режим «Экспедиция» спавнит на планете.
/// Группа становится доступной только спустя <see cref="UnlockAfter"/> после посадки шаттла,
/// поэтому чем дольше длится экспедиция, тем разнообразнее и опаснее становятся волны.
/// </summary>
[Prototype("pveHostileGroup")]
public sealed partial class PveHostileGroupPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>Какая сущность спавнится в группе.</summary>
    [DataField(required: true)]
    public EntProtoId Entity;

    /// <summary>Минимальный размер группы.</summary>
    [DataField]
    public int MinCount = 1;

    /// <summary>Максимальный размер группы.</summary>
    [DataField]
    public int MaxCount = 1;

    /// <summary>Через сколько после посадки группа разблокируется.</summary>
    [DataField]
    public TimeSpan UnlockAfter = TimeSpan.Zero;

    /// <summary>Вес при случайном выборе среди разблокированных групп.</summary>
    [DataField]
    public float Weight = 1f;
}
