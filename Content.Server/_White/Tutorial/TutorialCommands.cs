using Content.Server.Administration;
using Content.Server.Tutorial.Systems;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Player;
using System.Numerics;

namespace Content.Server.Tutorial.Commands;

[AdminCommand(AdminFlags.None)]
public sealed class TutorialCommand : IConsoleCommand
{
    public string Command => "tutorial";
    public string Description => "Teleports you to tutorial arena";
    public string Help => "tutorial";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not ICommonSession player)
        {
            shell.WriteError("This command can only be used by players.");
            return;
        }

        if (player.AttachedEntity == null)
        {
            shell.WriteError("You must have an entity to use this command.");
            return;
        }

        var entityManager = IoCManager.Resolve<IEntityManager>();
        var tutorialSystem = entityManager.System<TutorialArenaSystem>();
        var transformSystem = entityManager.System<SharedTransformSystem>();

        var (mapUid, gridUid) = tutorialSystem.AssertTutorialLoaded(player);
        transformSystem.SetCoordinates((EntityUid)player.AttachedEntity.Value,
            new EntityCoordinates(gridUid ?? mapUid, Vector2.One));

        shell.WriteLine("Teleported to tutorial arena!");
    }
}
