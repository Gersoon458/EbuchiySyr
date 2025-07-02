using Content.Shared.Tutorial;

namespace Content.Client.Tutorial;

public sealed class TutorialSystem : EntitySystem
{
    public void RequestTutorial()
    {
        RaiseNetworkEvent(new RequestTutorialEvent());
    }

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<TutorialResponseEvent>(OnTutorialResponse);
    }

    private void OnTutorialResponse(TutorialResponseEvent message, EntitySessionEventArgs args)
    {
        if (!message.Success && !string.IsNullOrEmpty(message.ErrorMessage))
        {
            Logger.Error($"Tutorial failed: {message.ErrorMessage}");
        }
    }
}
