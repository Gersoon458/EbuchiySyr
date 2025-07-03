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

    public const string TutorialMapPath = "/Maps/_White/Test/tutorial_arena.yml"; // Убедитесь, что этот путь верен

    public Dictionary<NetUserId, EntityUid> ArenaMap { get; private set; } = new();
    public Dictionary<NetUserId, EntityUid?> ArenaGrid { get; private set; } = new();

    public (EntityUid Map, EntityUid? Grid) AssertTutorialLoaded(ICommonSession player)
    {
        // Проверяем существующую карту
        if (ArenaMap.TryGetValue(player.UserId, out var tutorialMap) &&
            !Deleted(tutorialMap) && !Terminating(tutorialMap))
        {
            var mapComponent = Comp<MapComponent>(tutorialMap);
            // Проверяем, существует ли карта и инициализирована ли она
            if (_mapManager.MapExists(mapComponent.MapId) && _mapManager.IsMapInitialized(mapComponent.MapId))
            {
                if (ArenaGrid.TryGetValue(player.UserId, out var tutorialGrid) &&
                    tutorialGrid.HasValue && !Deleted(tutorialGrid.Value) && !Terminating(tutorialGrid.Value))
                {
                    return (tutorialMap, tutorialGrid);
                }
            }
            // Если карта существует, но есть проблемы, удаляем её
            _mapManager.DeleteMap(mapComponent.MapId);
            ArenaMap.Remove(player.UserId);
            ArenaGrid.Remove(player.UserId);
        }

        // Создаем новую карту
        var mapId = _mapManager.CreateMap();
        var newMapUid = _mapManager.GetMapEntityId(mapId);

        ArenaMap[player.UserId] = newMapUid;
        _metaDataSystem.SetEntityName(newMapUid, $"Tutorial-{player.Name}");

        // Загружаем содержимое карты из файла (LoadMap автоматически инициализирует карту)
        var grids = _map.LoadMap(mapId, TutorialMapPath);
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
