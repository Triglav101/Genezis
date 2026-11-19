namespace Content.Shared._Genezis.PveExpedition;

/// <summary>
/// Маркер цели задачи на уничтожение структур. На него наводятся пинпоинтеры режима «Экспедиция»
/// (<c>Pinpointer.component: PveTaskTarget</c>). Вешается правилом на структуры при спавне задачи.
/// </summary>
[RegisterComponent]
public sealed partial class PveTaskTargetComponent : Component
{
}
