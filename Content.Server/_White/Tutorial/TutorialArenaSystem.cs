using System.Collections.Generic;
using System.Numerics;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Content.Server.GameTicking;
using Robust.Server.Maps;

namespace Content.Server.Tutorial.Systems;

/// <summary>
/// Система для создания индивидуальных обучающих карт для новичков
/// </summary>
public sealed class TutorialArenaSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly MapLoaderSystem _map = default!;
    [Dependency] private readonly MetaDataSystem _metaDataSystem = default!;

    public const string TutorialMapPath = "/Maps/_White/Test/tutorial_arena.yml";

    public Dictionary<NetUserId, EntityUid> ArenaMap { get; private set; } = new();
    public Dictionary<NetUserId, EntityUid?> ArenaGrid { get; private set; } = new();

    public (EntityUid Map, EntityUid? Grid) AssertTutorialLoaded(ICommonSession player)
    {
        // Всегда создаем новую карту, предварительно удалив старую
        if (ArenaMap.TryGetValue(player.UserId, out var existingMap) && !Deleted(existingMap))
        {
            var mapId = Comp<MapComponent>(existingMap).MapId;
            _mapManager.DeleteMap(mapId);
        }

        // Создаем новую карту
        var newMapId = _mapManager.CreateMap();
        var newMapUid = _mapManager.GetMapEntityId(newMapId);

        ArenaMap[player.UserId] = newMapUid;
        _metaDataSystem.SetEntityName(newMapUid, $"Tutorial-{player.Name}");

        var grids = _map.LoadMap(newMapId, TutorialMapPath);
        _mapManager.SetMapPaused(newMapId, false);

        if (grids.Count != 0)
        {
            _metaDataSystem.SetEntityName(grids[0], $"TutorialGrid-{player.Name}");
            ArenaGrid[player.UserId] = grids[0];
        }
        else
        {
            ArenaGrid[player.UserId] = null;
        }

        return (ArenaMap[player.UserId], ArenaGrid[player.UserId]);
    }

    public void CleanupTutorial(NetUserId userId)
    {
        if (ArenaMap.TryGetValue(userId, out var mapUid) && !Deleted(mapUid))
        {
            var mapId = Comp<MapComponent>(mapUid).MapId;
            _mapManager.DeleteMap(mapId);
        }

        ArenaMap.Remove(userId);
        ArenaGrid.Remove(userId);
    }
}
