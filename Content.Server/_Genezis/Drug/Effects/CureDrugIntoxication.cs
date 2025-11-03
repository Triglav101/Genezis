using Content.Server._Genezis.Drug.Components;
using Content.Server._Genezis.Drug;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Server._Genezis.Drug.Effects;

public sealed partial class CureDrugIntoxication : EntityEffect
{
    [DataField]
    public float HealStrenght = -1;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("reagent-effect-guidebook-cure-drug-intoxication", ("chance", Probability));
    }

    public override void Effect(EntityEffectBaseArgs args)
    {
        var entityManager = args.EntityManager;

        var instantDrugAddication = entityManager.EnsureComponent<InstantDrugAddicationComponent>(args.TargetEntity);

        args.EntityManager.System<DrugAddicationSystem>().AddTimeLastAppointment(
            args.TargetEntity,
            HealStrenght
            );
    }
}

