using Content.Shared.Tutorial;
using Robust.Shared.Network;

namespace Content.Client.Tutorial;

public sealed class TutorialSystem : EntitySystem
{
    [Dependency] private readonly IClientNetManager _netManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        _netManager.RegisterNetMessage<RequestTutorialMessage>();
        _netManager.RegisterNetMessage<TutorialResponseMessage>(OnTutorialResponse);
    }

    public void RequestTutorial()
    {
        var message = new RequestTutorialMessage();
        _netManager.ClientSendMessage(message);
    }

    private void OnTutorialResponse(TutorialResponseMessage message)
    {
        if (!message.Success && !string.IsNullOrEmpty(message.ErrorMessage))
        {
            Logger.Error($"Tutorial failed: {message.ErrorMessage}");
        }
    }
}
