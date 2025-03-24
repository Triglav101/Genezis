using Content.Server.GameTicking;
using LightCycleComponent = Content.Shared._Genezis.DayCycle.LightCycleComponent;

namespace Content.Server._Genezis.DayCycle
{
    public sealed partial class LightCycleSystem : EntitySystem
    {
        [Dependency] private readonly GameTicker _gameTicker = default!;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<LightCycleComponent, ComponentStartup>(OnComponentStartup);
        }

        private void OnComponentStartup(EntityUid uid, LightCycleComponent cycle, ComponentStartup args)
        {
            cycle.Offset = _gameTicker.RoundDuration();
        }

    }

}
