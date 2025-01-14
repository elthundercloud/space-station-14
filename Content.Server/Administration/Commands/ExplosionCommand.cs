using Content.Server.Administration.UI;
using Content.Server.EUI;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.Administration;
using Content.Shared.Explosion;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Fun)]
public sealed class OpenExplosionEui : IConsoleCommand
{
    public string Command => "explosionui";
    public string Description => "Opens a window for easy access to station destruction";
    public string Help => $"Usage: {Command}";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var player = shell.Player as IPlayerSession;
        if (player == null)
        {
            shell.WriteLine("This does not work from the server console.");
            return;
        }

        var eui = IoCManager.Resolve<EuiManager>();
        var ui = new SpawnExplosionEui();
        eui.OpenEui(ui, player);
    }
}

[AdminCommand(AdminFlags.Fun)] // for the admin. Not so much for anyone else.
public sealed class ExplosionCommand : IConsoleCommand
{
    public string Command => "explosion";
    public string Description => "Train go boom";

    // Note that if you change the arguments, you should also update the client-side SpawnExplosionWindow, as that just
    // uses this command.
    public string Help => "Usage: explosion intensity [slope] [maxIntensity] [x y] [mapId] [prototypeId]";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0 || args.Length == 4 || args.Length > 7)
        {
            shell.WriteLine("Wrong number of arguments.");
            return;
        }

        if (!float.TryParse(args[0], out var intensity))
        {
            shell.WriteLine($"Failed to parse intensity: {args[0]}");
            return;
        }
 
        float slope = 5;
        if (args.Length > 1 && !float.TryParse(args[1], out slope))
        {
            shell.WriteLine($"Failed to parse float: {args[1]}");
            return;
        }

        float maxIntensity = 100;
        if (args.Length > 2 && !float.TryParse(args[2], out maxIntensity))
        {
            shell.WriteLine($"Failed to parse float: {args[2]}");
            return;
        }

        float x = 0, y = 0;
        if (args.Length > 4)
        {
            if (!float.TryParse(args[3], out x) ||
                !float.TryParse(args[4], out y))
            {
                shell.WriteLine($"Failed to parse coordinates: {(args[3], args[4])}");
                return;
            }
        }

        MapCoordinates coords;
        if (args.Length > 5)
        {
            if (!int.TryParse(args[5], out var parsed))
            {
                shell.WriteLine($"Failed to parse map ID: {args[5]}");
                return;
            }
            coords = new MapCoordinates((x, y), new(parsed));
        }
        else
        {
            // attempt to find the player's current position
            var entMan = IoCManager.Resolve<IEntityManager>();
            if (!entMan.TryGetComponent(shell.Player?.AttachedEntity, out TransformComponent? xform))
            {
                shell.WriteLine($"Failed get default coordinates/map via player's transform. Need to specify explicitly.");
                return;
            }

            if (args.Length > 4)
                coords = new MapCoordinates((x, y), xform.MapID);
            else
                coords = xform.MapPosition;

        }

        ExplosionPrototype? type;
        var protoMan = IoCManager.Resolve<IPrototypeManager>();
        if (args.Length > 6)
        {
            if (!protoMan.TryIndex(args[6], out type))
            {
                shell.WriteLine($"Unknown explosion prototype: {args[6]}");
                return;
            }
        }
        else
        {
            // no prototype was specified, so lets default to whichever one was defined first
            var types = protoMan.EnumeratePrototypes<ExplosionPrototype>();
            if (!types.Any())
                return;

            type = types.First();
        }

        EntitySystem.Get<ExplosionSystem>().QueueExplosion(coords, type.ID, intensity, slope, maxIntensity);
    }
}
