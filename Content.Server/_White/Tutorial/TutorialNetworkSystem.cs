using Content.Server.Tutorial.Systems;
using Content.Shared.Tutorial;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using System.Numerics;

namespace Content.Server.Tutorial.Systems;

public sealed class TutorialNetworkSystem : EntitySystem
{
    [Dependency] private readonly TutorialArenaSystem _tutorialArena = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly INetManager _netManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        _netManager.RegisterNetMessage<RequestTutorialMessage>();
        _netManager.RegisterNetMessage<TutorialResponseMessage>();

        SubscribeNetworkEvent<RequestTutorialMessage>(OnRequestTutorial);
    }

    private void OnRequestTutorial(RequestTutorialMessage message, EntitySessionEventArgs args)
    {
        if (args.SenderSession is not ICommonSession player)
            return;

        if (player.AttachedEntity == null)
        {
            var response = new TutorialResponseMessage
            {
                Success = false,
                ErrorMessage = "No attached entity"
            };
            _netManager.ServerSendMessage(response, player.Channel);
            return;
        }

        try
        {
            var (mapUid, gridUid) = _tutorialArena.AssertTutorialLoaded(player);
            _transform.SetCoordinates((EntityUid)player.AttachedEntity.Value,
                new EntityCoordinates(gridUid ?? mapUid, Vector2.One));

            var successResponse = new TutorialResponseMessage { Success = true };
            _netManager.ServerSendMessage(successResponse, player.Channel);
        }
        catch (Exception ex)
        {
            var errorResponse = new TutorialResponseMessage
            {
                Success = false,
                ErrorMessage = ex.Message
            };
            _netManager.ServerSendMessage(errorResponse, player.Channel);
        }
    }
}
