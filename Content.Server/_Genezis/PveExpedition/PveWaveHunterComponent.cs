namespace Content.Server._Genezis.PveExpedition;

/// <summary>
///     Вешается на врагов из волн режима «Экспедиция» (НЕ на цели задач).
///     Моб всегда знает, где ближайший живой игрок на планете, идёт к нему и ломает преграды на пути.
/// </summary>
[RegisterComponent]
public sealed partial class PveWaveHunterComponent : Component
{
    /// <summary>Когда в следующий раз пересматривать цель этого моба.</summary>
    [ViewVariables]
    public TimeSpan NextUpdate;

    /// <summary>Игрок, к которому мы сейчас направили моба.</summary>
    [ViewVariables]
    public EntityUid? Chasing;
}
