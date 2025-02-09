using System.Numerics;
using Content.Client.Pinpointer.UI;
using Robust.Client.Graphics;

namespace Content.Client.StationTeleporter;

public sealed partial class StationTeleporterNavMapControl : NavMapControl
{
    public HashSet<(Vector2, Vector2)> LinkedTeleportersCoordinates = new();

    private readonly SharedTransformSystem _transformSystem;

    public StationTeleporterNavMapControl() : base()
    {
        _transformSystem = EntManager.System<SharedTransformSystem>();

        MaxSelectableDistance = 30f;

        WallColor = new Color(32, 96, 128);
        TileColor = new Color(12, 50, 69);
        BackgroundColor = Color.FromSrgb(TileColor.WithAlpha(BackgroundOpacity));

    }

}
