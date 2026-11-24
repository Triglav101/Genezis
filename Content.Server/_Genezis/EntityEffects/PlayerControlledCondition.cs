using Content.Shared.EntityEffects;
using JetBrains.Annotations;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Genezis.EntityEffects;

/// <summary>
///     Условие: сущность в данный момент управляется игроком (к ней подключена сессия).
///     Нужно, чтобы эффекты газа не действовали на мобов без игрока (NPC, отключившиеся игроки).
/// </summary>
[UsedImplicitly]
public sealed partial class PlayerControlled : EntityEffectCondition
{
    /// <summary>
    ///     true - условие выполняется для сущностей БЕЗ игрока.
    /// </summary>
    [DataField]
    public bool Invert;

    public override bool Condition(EntityEffectBaseArgs args)
    {
        return args.EntityManager.HasComponent<ActorComponent>(args.TargetEntity) ^ Invert;
    }

    public override string GuidebookExplanation(IPrototypeManager prototype)
    {
        return Loc.GetString("reagent-effect-condition-guidebook-player-controlled", ("invert", Invert));
    }
}
