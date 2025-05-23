﻿using Content.Shared._Sunrise.FleshCult;
using Content.Shared.Flesh;
using Robust.Client.GameObjects;

namespace Content.Client._Sunrise.FleshCult;

public sealed class FleshHeartSystem : VisualizerSystem<FleshHeartComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, FleshHeartComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!AppearanceSystem.TryGetData<FleshHeartStatus>(uid, FleshHeartVisuals.State, out var state, args.Component))
            return;
        var layer = args.Sprite.LayerMapGet(FleshHeartLayers.Base);

        if (state == FleshHeartStatus.Active)
        {
            args.Sprite.LayerSetState(layer, component.FinalState);
        }
    }
}
