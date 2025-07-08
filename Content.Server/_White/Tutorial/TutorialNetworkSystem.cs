using System.Numerics;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Content.Server.GameTicking;
using Content.Server.Station.Systems;
using Content.Shared.Mind;
using Content.Server.Chat.Managers;
using Content.Shared.Tutorial;
using Robust.Shared.Random;
using Content.Server.Mind.Commands;
using Content.Server.Spawners.Components;

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
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;

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
            // Проверяем наличие существующей карты для игрока
            if (_tutorialArena.ArenaMap.ContainsKey(player.UserId))
            {
                // Если карта существует, удаляем её
                _tutorialArena.CleanupTutorial(player.UserId);
            }

            // Создаем новую карту
            var (mapUid, gridUid) = _tutorialArena.AssertTutorialLoaded(player);

            // Впускаем игрока в игру (аналогично PlayerJoinGame)
            _gameTicker.PlayerJoinGame(player);

            // Ищем спавнер таракана на карте
            EntityCoordinates? spawnCoordinates = null;
            var spawnerQuery = EntityQueryEnumerator<ConditionalSpawnerComponent, TransformComponent>();
            while (spawnerQuery.MoveNext(out var spawnerUid, out var spawner, out var spawnerXform))
            {
                if (spawnerXform.MapUid == mapUid &&
                    MetaData(spawnerUid).EntityPrototype?.ID == "MarkerMobTutorial")
                {
                    spawnCoordinates = spawnerXform.Coordinates;
                    break;
                }
            }

            // Если спавнер не найден, используем центр грида
            if (spawnCoordinates == null && gridUid.HasValue)
            {
                spawnCoordinates = new EntityCoordinates(gridUid.Value, Vector2.Zero);
            }

            if (spawnCoordinates.HasValue)
            {
                // Создаем Mind для игрока (аналогично SpawnObserver)
                var name = _gameTicker.GetPlayerProfile(player).Name;
                var (mindId, mindComp) = _mindSystem.CreateMind(player.UserId, name);
                _mindSystem.SetUserId(mindId, player.UserId);  // Pass only mindId, not the tuple

                // Спавним таракана на месте спавнера (аналогично SpawnPlayerMob)
                var cockroach = Spawn("MobTutorial", spawnCoordinates.Value);

                // Делаем таракана разумным
                MakeSentientCommand.MakeSentient(cockroach, EntityManager, true, true);

                // Передаем управление игроку
                _mindSystem.TransferTo(mindId, cockroach);

                RaiseNetworkEvent(new TutorialResponseEvent { Success = true }, args.SenderSession);
            }
            else
            {
                throw new Exception("No spawn location found for cockroach");
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Tutorial failed for {player.Name}: {ex}");

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
