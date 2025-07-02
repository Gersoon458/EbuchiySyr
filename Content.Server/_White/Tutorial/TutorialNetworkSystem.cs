using Content.Server.GameTicking;
using Content.Server.Station.Systems;
using Content.Server.Tutorial.Systems;
using Content.Shared.Tutorial;
using Content.Shared.Players;
using Content.Shared.Mind;
using Content.Server.Chat.Managers;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using System.Numerics;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.Tutorial.Systems;

public sealed class TutorialNetworkSystem : EntitySystem
{
    [Dependency] private readonly TutorialArenaSystem _tutorialArena = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<RequestTutorialEvent>(OnRequestTutorial);
    }

    private void OnRequestTutorial(RequestTutorialEvent message, EntitySessionEventArgs args)
    {
        if (args.SenderSession is not ICommonSession player)
            return;

        // Проверка на наличие раунда
        if (_gameTicker.RunLevel != GameRunLevel.InRound)
        {
            _chatManager.DispatchServerMessage(player,
                "Обучение доступно только во время раунда. Попробуйте позже, когда начнется раунд.");

            RaiseNetworkEvent(new TutorialResponseEvent
            {
                Success = false,
                ErrorMessage = "Round not started"
            }, args.SenderSession);
            return;
        }

        try
        {
            // Сначала переводим игрока в раунд
            _gameTicker.PlayerJoinGame(player);

            // Создаем карту
            var (mapUid, gridUid) = _tutorialArena.AssertTutorialLoaded(player);

            // КРИТИЧЕСКИ ВАЖНАЯ ПРОВЕРКА
            var mapComponent = EntityManager.GetComponent<MapComponent>(mapUid);
            if (!_mapManager.MapExists(mapComponent.MapId))
            {
                throw new InvalidOperationException($"Tutorial map {mapComponent.MapId} does not exist after creation");
            }

            // Ждем один тик для синхронизации
            Timer.Spawn(100, () =>
            {
                // Логика создания тела и телепортации
                if (player.AttachedEntity == null)
                {
                    var profile = _gameTicker.GetPlayerProfile(player);
                    var spawnCoords = new EntityCoordinates(gridUid ?? mapUid, Vector2.One);

                    var mobEntity = _stationSpawning.SpawnPlayerMob(
                        spawnCoords,
                        "Passenger",
                        profile,
                        null
                    );

                    var data = player.ContentData();
                    if (data?.Mind != null)
                    {
                        var mindSystem = EntityManager.System<SharedMindSystem>();
                        mindSystem.TransferTo(data.Mind.Value, mobEntity);
                    }
                }
                else
                {
                    _transform.SetCoordinates((EntityUid)player.AttachedEntity.Value,
                        new EntityCoordinates(gridUid ?? mapUid, Vector2.One));
                }

                RaiseNetworkEvent(new TutorialResponseEvent { Success = true }, args.SenderSession);
            });
        }
        catch (Exception ex)
        {
            RaiseNetworkEvent(new TutorialResponseEvent
            {
                Success = false,
                ErrorMessage = ex.Message
            }, args.SenderSession);
        }
    }
}
