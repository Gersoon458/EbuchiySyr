using System.Numerics;
using Content.Shared.IconSmoothing;
using Robust.Client.GameObjects;

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
        // White Dream Edit Start
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
        // White Dream Edit End
    }

    private void OnEdgeShutdown(EntityUid uid, SmoothEdgeComponent component, ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;
        // White Dream Edit Start
        var allEdgeLayers = Enum.GetValues<EdgeLayer>();
        foreach (var edgeLayer in allEdgeLayers)
        {
            if (sprite.LayerMapTryGet(edgeLayer, out _))
                sprite.LayerMapRemove(edgeLayer);
        }
        // White Dream Edit End
    }

    private void CalculateEdge(EntityUid uid, DirectionFlag directions, SpriteComponent? sprite = null, SmoothEdgeComponent? component = null)
    {
        if (!Resolve(uid, ref sprite, ref component, false))
            return;
        // White Dream Edit Start
        var directionMappings = new Dictionary<DirectionFlag, EdgeLayer>
        {
            { DirectionFlag.South, EdgeLayer.South },
            { DirectionFlag.East, EdgeLayer.East },
            { DirectionFlag.North, EdgeLayer.North },
            { DirectionFlag.West, EdgeLayer.West }
        };

        if (Enum.IsDefined(typeof(DirectionFlag), "SouthEast"))
        {
            directionMappings.Add((DirectionFlag)Enum.Parse(typeof(DirectionFlag), "SouthEast"), EdgeLayer.SouthEast);
            directionMappings.Add((DirectionFlag)Enum.Parse(typeof(DirectionFlag), "NorthEast"), EdgeLayer.NorthEast);
            directionMappings.Add((DirectionFlag)Enum.Parse(typeof(DirectionFlag), "NorthWest"), EdgeLayer.NorthWest);
            directionMappings.Add((DirectionFlag)Enum.Parse(typeof(DirectionFlag), "SouthWest"), EdgeLayer.SouthWest);
        }

        foreach (var (dir, edge) in directionMappings)
        {
            if (!sprite.LayerMapTryGet(edge, out _))
                continue;
        // White Dream Edit End

            if ((dir & directions) != 0x0)
            {
                sprite.LayerSetVisible(edge, false);
                continue;
            }

            sprite.LayerSetVisible(edge, true);
        }
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
                // White Dream Edit Start
                if (Enum.IsDefined(typeof(EdgeLayer), "SouthEast"))
                {
                    var dirName = direction.ToString();
                    if (Enum.TryParse<EdgeLayer>(dirName, out var edgeLayer))
                        return edgeLayer;
                }
                // White Dream Edit End
                throw new ArgumentOutOfRangeException();
        }
    }

    private enum EdgeLayer : byte
    {
        South,
        East,
        North,
        West,
        // White Dream Edit Start
        SouthEast,
        NorthEast,
        NorthWest,
        SouthWest
        // White Dream Edit End
    }
}
