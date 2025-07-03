using System.Collections.Generic;
using System.Numerics;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Content.Server.GameTicking;
using Robust.Server.Maps;
using Content.Server.Station.Systems;
using Content.Shared.Players;
using Content.Shared.Mind;
using Content.Server.Chat.Managers;
using Robust.Shared.Console;
using Robust.Shared.Timing;
using Content.Shared.Tutorial;
using Content.Server.Spawners.EntitySystems;
using Content.Server.Spawners.Components;
using Robust.Shared.Random;

namespace Content.Server.Tutorial.Systems;

public sealed class TutorialNetworkSystem : EntitySystem
{
    [Dependency] private readonly TutorialArenaSystem _tutorialArena = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MetaDataSystem _metaDataSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<RequestTutorialEvent>(OnRequestTutorial);
    }

    private void OnRequestTutorial(RequestTutorialEvent message, EntitySessionEventArgs args)
    {
        if (args.SenderSession is not ICommonSession player)
            return;

        if (_gameTicker.RunLevel != GameRunLevel.InRound)
        {
            _chatManager.DispatchServerMessage(
                player,
                "Обучение доступно только во время раунда. Попробуйте позже, когда начнется раунд.");

            RaiseNetworkEvent(
                new TutorialResponseEvent
                {
                    Success = false,
                    ErrorMessage = "Round not started"
                },
                args.SenderSession);
            return;
        }

        try
        {
            var (mapUid, gridUid) = _tutorialArena.AssertTutorialLoaded(player);

            if (!Exists(mapUid))
            {
                throw new Exception("Failed to create tutorial map");
            }

            EntityCoordinates spawnCoords;
            var possibleSpawnPoints = new List<EntityCoordinates>();
            var query = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();
            while (query.MoveNext(out var uid, out var spawnPoint, out var xform))
            {
                if (xform.MapUid == mapUid)
                {
                    possibleSpawnPoints.Add(xform.Coordinates);
                }
            }

            if (possibleSpawnPoints.Count > 0)
            {
                spawnCoords = _random.Pick(possibleSpawnPoints);
            }
            else
            {
                spawnCoords = new EntityCoordinates(gridUid ?? mapUid, Vector2.One);
                Logger.Warning($"No spawn points found on tutorial map {(mapUid.IsValid() ? Comp<MetaDataComponent>(mapUid).EntityName : "Invalid MapUid")}. Spawning at {spawnCoords}.");
            }

            if (player.AttachedEntity == null)
            {
                _gameTicker.MakeJoinGame(player, EntityUid.Invalid, "Passenger");

                if (player.AttachedEntity != null)
                {
                    _transform.SetCoordinates((EntityUid)player.AttachedEntity.Value, spawnCoords);
                }
            }
            else
            {
                _transform.SetCoordinates((EntityUid) player.AttachedEntity.Value, spawnCoords);
            }

            RaiseNetworkEvent(new TutorialResponseEvent { Success = true }, args.SenderSession);
        }
        catch (Exception ex)
        {
            Logger.Error($"Tutorial failed for {player.Name}: {ex}");
            // Удалена строка: _chatManager.DispatchServerMessage(player, $"Ошибка при создании обучения: {ex.Message}");

            RaiseNetworkEvent(
                new TutorialResponseEvent
                {
                    Success = false,
                    ErrorMessage = ex.Message
                },
                args.SenderSession);
        }
    }
}
