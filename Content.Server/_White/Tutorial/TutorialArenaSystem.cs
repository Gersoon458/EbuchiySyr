using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server.Tutorial.Systems;

public sealed class TutorialArenaSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly MapLoaderSystem _map = default!;
    [Dependency] private readonly MetaDataSystem _metaDataSystem = default!;

    public const string TutorialMapPath = "/Maps/_White/Test/tutorial_arena.yml";

    public Dictionary<NetUserId, EntityUid> TutorialMap { get; private set; } = new();
    public Dictionary<NetUserId, EntityUid?> TutorialGrid { get; private set; } = new();

    public (EntityUid Map, EntityUid? Grid) AssertTutorialLoaded(ICommonSession player)
    {
        if (TutorialMap.TryGetValue(player.UserId, out var tutorialMap) && !Deleted(tutorialMap) && !Terminating(tutorialMap))
        {
            if (TutorialGrid.TryGetValue(player.UserId, out var tutorialGrid) && !Deleted(tutorialGrid) && !Terminating(tutorialGrid.Value))
            {
                return (tutorialMap, tutorialGrid);
            }
            else
            {
                TutorialGrid[player.UserId] = null;
                return (tutorialMap, null);
            }
        }

        TutorialMap[player.UserId] = _mapManager.GetMapEntityId(_mapManager.CreateMap());
        _metaDataSystem.SetEntityName(TutorialMap[player.UserId], $"Tutorial-{player.Name}");
        var grids = _map.LoadMap(Comp<MapComponent>(TutorialMap[player.UserId]).MapId, TutorialMapPath);
        if (grids.Count != 0)
        {
            _metaDataSystem.SetEntityName(grids[0], $"TutorialGrid-{player.Name}");
            TutorialGrid[player.UserId] = grids[0];
        }
        else
        {
            TutorialGrid[player.UserId] = null;
        }

        return (TutorialMap[player.UserId], TutorialGrid[player.UserId]);
    }

    public void CleanupTutorial(NetUserId userId)
    {
        if (TutorialMap.TryGetValue(userId, out var mapUid) && !Deleted(mapUid))
        {
            var mapId = Comp<MapComponent>(mapUid).MapId;
            _mapManager.DeleteMap(mapId);
        }

        TutorialMap.Remove(userId);
        TutorialGrid.Remove(userId);
    }
}
