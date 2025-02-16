using Content.Shared._RMC14.Xenonids.Fruit;
using Content.Shared._RMC14.Xenonids.Fruit.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Xenonids.Fruit;


[UsedImplicitly]
public sealed class XenoFruitChooseBui : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly SpriteSystem _sprite;
    private readonly SharedXenoFruitSystem _xenoFruit;

    [ViewVariables]
    private XenoFruitChooseWindow? _window;

    public XenoFruitChooseBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _sprite = EntMan.System<SpriteSystem>();
        _xenoFruit = EntMan.System<SharedXenoFruitSystem>();
    }

    protected override void Open()
    {
        _window = new XenoFruitChooseWindow();
        _window.OnClose += Close;

        var group = new ButtonGroup();
        if (EntMan.TryGetComponent(Owner, out XenoFruitPlanterComponent? xeno))
        {
            _window.FruitCountLabel.Text = Loc.GetString("rmc-xeno-fruit-ui-count", ("count", xeno.PlantedFruit.Count), ("max", xeno.MaxFruitAllowed));

            foreach (var fruitId in xeno.CanPlant)
            {
                if (!_prototype.TryIndex(fruitId, out var fruit))
                    continue;

                var name = fruit.Name;
            }
        }

        Refresh();
        _window.OpenCentered();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _window?.Dispose();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is XenoFruitChooseBuiState uiState)
            UpdateState(uiState);
    }

    private void UpdateState(XenoFruitChooseBuiState state)
    {
        if (_window == null)
            return;

        _window.FruitCountLabel.Text = Loc.GetString("rmc-xeno-fruit-ui-count", ("count", state.Count), ("max", state.Max));
    }

    public void Refresh()
    {
        if (!EntMan.TryGetComponent(Owner, out XenoFruitPlanterComponent? xeno) ||
            _window == null)
            return;

        if (xeno.FruitChoice is not { } choice)
            return;

    }
}
