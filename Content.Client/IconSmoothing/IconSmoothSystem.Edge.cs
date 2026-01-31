using System.Numerics;
using Content.Shared.IconSmoothing;
using Robust.Client.GameObjects;
using System.Linq; // WWDP edit
using System.Collections.Generic; // WWDP edit

namespace Content.Client.IconSmoothing;

public sealed partial class IconSmoothSystem
{
    // Handles drawing edge sprites on the non-smoothed edges.

    private void InitializeEdge()
    {
        SubscribeLocalEvent<SmoothEdgeComponent, ComponentStartup>(OnEdgeStartup);
        SubscribeLocalEvent<SmoothEdgeComponent, ComponentShutdown>(OnEdgeShutdown);
    }

    private void OnEdgeStartup(EntityUid uid, SmoothEdgeComponent component, ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        // WWDPEdit Start
        var edgeOffsets = new Dictionary<EdgeLayer, Vector2>
        {
            { EdgeLayer.South, new Vector2(0, -1f) },
            { EdgeLayer.East, new Vector2(1f, 0f) },
            { EdgeLayer.North, new Vector2(0, 1f) },
            { EdgeLayer.West, new Vector2(-1f, 0f) },
            { EdgeLayer.SouthEast, new Vector2(1f, -1f) },
            { EdgeLayer.NorthEast, new Vector2(1f, 1f) },
            { EdgeLayer.NorthWest, new Vector2(-1f, 1f) },
            { EdgeLayer.SouthWest, new Vector2(-1f, -1f) }
        };

        foreach (var (edgeLayer, offset) in edgeOffsets)
        {
            if (sprite.LayerMapTryGet(edgeLayer, out _))
            {
                sprite.LayerSetOffset(edgeLayer, offset);
                sprite.LayerSetVisible(edgeLayer, false);
            }
        }
        // WWDP Edit End
    }

    private void OnEdgeShutdown(EntityUid uid, SmoothEdgeComponent component, ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;
        // WWDP Edit Start
        var allEdgeLayers = Enum.GetValues<EdgeLayer>();
        foreach (var edgeLayer in allEdgeLayers)
        {
            if (sprite.LayerMapTryGet(edgeLayer, out _))
                sprite.LayerMapRemove(edgeLayer);
        }
        // WWDP Edit End
    }

    private void CalculateEdge(EntityUid uid, DirectionFlag directions, SpriteComponent? sprite = null, SmoothEdgeComponent? component = null)
    {
        if (!Resolve(uid, ref sprite, ref component, false))
            return;

        if (component.DrawDepth.HasValue)
        {
            sprite.DrawDepth = component.DrawDepth.Value;
        }

        var xform = Transform(uid);
        if (!TryComp<Robust.Shared.Map.Components.MapGridComponent>(xform.GridUid, out var grid))
            return;

        var pos = grid.TileIndicesFor(xform.Coordinates);
        var smoothQuery = GetEntityQuery<IconSmoothComponent>();

        var directionMappings = new[]
        {
            (DirectionFlag.South, EdgeLayer.South),
            (DirectionFlag.East, EdgeLayer.East),
            (DirectionFlag.North, EdgeLayer.North),
            (DirectionFlag.West, EdgeLayer.West)
        };

        foreach (var (dir, edge) in directionMappings)
        {
            if (!sprite.LayerMapTryGet(edge, out var layerIndex))
                continue;

            var neighborPos = pos + DirectionToOffset(dir);
            var hasMatchingNeighbor = false;
            var enumerator = grid.GetAnchoredEntitiesEnumerator(neighborPos);

            while (enumerator.MoveNext(out var neighbor))
            {
                if (smoothQuery.TryGetComponent(neighbor, out var neighborSmooth) &&
                    neighborSmooth != null &&
                    neighborSmooth.Enabled &&
                    MatchesEdgeCriteria(component, neighborSmooth))
                {
                    hasMatchingNeighbor = true;
                    break;
                }
            }

            // Для RequireMatchingKey = true показываем только при наличии совпадения
            // Для RequireMatchingKey = false показываем при отсутствии соседей (старая логика)
            var shouldShowEdge = component.RequireMatchingKey ?
                hasMatchingNeighbor :
                !hasMatchingNeighbor;

            sprite.LayerSetVisible(layerIndex, shouldShowEdge);
        }
    }

    private bool MatchesEdgeCriteria(SmoothEdgeComponent edge, IconSmoothComponent neighbor)
    {
        // Если RequireMatchingKey не установлен, используем старую логику
        if (!edge.RequireMatchingKey)
            return true; // Всегда показываем edge для обычных сущностей

        // Новая логика для условных edge-спрайтов
        if (edge.EdgeSubKeys == null || neighbor.SmoothKey == null)
            return false;

        return edge.EdgeSubKeys.Any(subKey =>
            !string.IsNullOrEmpty(subKey) && neighbor.SmoothKey == subKey);
    }

    private Vector2i DirectionToOffset(DirectionFlag direction)
    {
        return direction switch
        {
            DirectionFlag.North => new Vector2i(0, 1),
            DirectionFlag.South => new Vector2i(0, -1),
            DirectionFlag.East => new Vector2i(1, 0),
            DirectionFlag.West => new Vector2i(-1, 0),
            DirectionFlag.NorthEast => new Vector2i(1, 1),
            DirectionFlag.NorthWest => new Vector2i(-1, 1),
            DirectionFlag.SouthEast => new Vector2i(1, -1),
            DirectionFlag.SouthWest => new Vector2i(-1, -1),
            _ => Vector2i.Zero
        };
    }

    private EdgeLayer GetEdge(DirectionFlag direction)
    {
        switch (direction)
        {
            case DirectionFlag.South:
                return EdgeLayer.South;
            case DirectionFlag.East:
                return EdgeLayer.East;
            case DirectionFlag.North:
                return EdgeLayer.North;
            case DirectionFlag.West:
                return EdgeLayer.West;
            default:
                // WWDP Edit Start
                if (Enum.IsDefined(typeof(EdgeLayer), "SouthEast"))
                {
                    var dirName = direction.ToString();
                    if (Enum.TryParse<EdgeLayer>(dirName, out var edgeLayer))
                        return edgeLayer;
                }
                // WWDP Edit End
                throw new ArgumentOutOfRangeException();
        }
    }

    private enum EdgeLayer : byte
    {
        South,
        East,
        North,
        West,
        // WWDP Edit Start
        SouthEast,
        NorthEast,
        NorthWest,
        SouthWest
        // WWDP Edit End
    }
}
