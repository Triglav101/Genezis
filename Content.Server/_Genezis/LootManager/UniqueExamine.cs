using Content.Server._Genezis.Despawn;
using Content.Shared.Examine;

namespace Content.Server._Genezis.LootManager;

public sealed class UniqueExamine: EntitySystem
{
    [Dependency] private readonly LootManagerSystem _lootManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DespawnItemComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(EntityUid uid, DespawnItemComponent component, ExaminedEvent args)
    {
        if (!TryComp<MetaDataComponent>(args.Examined, out var metaData) || metaData.EntityPrototype == null)
            return;

        if (!_lootManager.LootManager.TryGetValue(metaData.EntityPrototype.ID, out var value))
            return;

        if (value.MaxCount == 1)
        {
            args.PushMarkup("[color=red][italic]>>> Уникальные <<<[/italic][/color]", 200);
        }
    }
}
