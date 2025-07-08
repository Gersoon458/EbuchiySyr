using Content.Client.Tutorial;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Localization;
using System.Numerics;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Lobby.UI;

public sealed class TutorialWarningWindow : DefaultWindow
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public readonly Button ConfirmButton;
    public readonly Button CancelButton;

    public TutorialWarningWindow()
    {
        IoCManager.InjectDependencies(this);

        Title = Loc.GetString("tutorial-warning-window-title");
        MinSize = new Vector2(400, 200);
        SetSize = new Vector2(400, 200);

        Contents.AddChild(new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            Margin = new Thickness(10),
            Children =
            {
                new Label
                {
                    Text = Loc.GetString("tutorial-warning-window-text"),
                    HorizontalAlignment = HAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 20)
                },
                new BoxContainer
                {
                    Orientation = LayoutOrientation.Horizontal,
                    HorizontalAlignment = HAlignment.Center,
                    SeparationOverride = 10,
                    Children =
                    {
                        (ConfirmButton = new Button
                        {
                            Text = Loc.GetString("tutorial-warning-window-confirm"),
                            MinSize = new Vector2(100, 30)
                        }),
                        (CancelButton = new Button
                        {
                            Text = Loc.GetString("tutorial-warning-window-cancel"),
                            MinSize = new Vector2(100, 30)
                        })
                    }
                }
            }
        });

        ConfirmButton.OnPressed += OnConfirmPressed;
        CancelButton.OnPressed += OnCancelPressed;
    }

    private void OnConfirmPressed(BaseButton.ButtonEventArgs args)
    {
        var tutorialSystem = _entityManager.System<TutorialSystem>();
        tutorialSystem.RequestTutorial();
        Close();
    }

    private void OnCancelPressed(BaseButton.ButtonEventArgs args)
    {
        Close();
    }
}
