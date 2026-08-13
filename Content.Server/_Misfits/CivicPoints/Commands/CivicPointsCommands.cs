using System.Linq;
using System.Threading.Tasks;
using Content.Server._Misfits.Characters;
using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Shared._Misfits.CivicPoints;
using Content.Shared.Administration;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server._Misfits.CivicPoints.Commands;

public abstract partial class CivicPointsCommandBase : LocalizedCommands
{
    [Dependency] protected IAdminLogManager AdminLog = default!;
    [Dependency] protected IPermanentCharacterManager Characters = default!;
    [Dependency] protected ICivicPointsManager CivicPoints = default!;
    [Dependency] protected IPlayerLocator PlayerLocator = default!;
    [Dependency] protected IPlayerManager Players = default!;

    protected async Task<CharacterSummary?> ResolveCharacter(
        IConsoleShell shell,
        string player,
        string slotText)
    {
        var playerData = await PlayerLocator.LookupIdByNameOrIdAsync(player);
        if (playerData == null)
        {
            shell.WriteError(Loc.GetString("cmd-civicpoints-player-not-found", ("player", player)));
            return null;
        }

        if (!int.TryParse(slotText, out var slot) || slot < 0)
        {
            shell.WriteError(Loc.GetString("cmd-civicpoints-invalid-slot", ("slot", slotText)));
            return null;
        }

        var characters = await Characters.GetCharactersAsync(playerData.UserId);
        var character = characters.SingleOrDefault(candidate => candidate.Slot == slot);
        if (character == null)
        {
            shell.WriteError(Loc.GetString("cmd-civicpoints-character-not-found",
                ("player", player),
                ("slot", slot)));
        }

        return character;
    }

    protected void WritePoints(IConsoleShell shell, CharacterSummary character, long points)
    {
        var title = Loc.GetString(CivicPointsRules.GetPriority(points).TitleLocId());
        shell.WriteLine(Loc.GetString("cmd-civicpoints-value",
            ("character", character.Name),
            ("characterId", character.Id.Value),
            ("slot", character.Slot),
            ("points", points),
            ("priority", title)));
    }

    protected void WriteChange(IConsoleShell shell, CivicPointsChange change, string operation, string reason)
    {
        shell.WriteLine(Loc.GetString("cmd-civicpoints-change-success",
            ("operation", operation),
            ("character", change.Character.Name),
            ("characterId", change.Character.Id.Value),
            ("oldPoints", change.PreviousPoints),
            ("newPoints", change.NewPoints),
            ("priority", Loc.GetString(change.NewPriority.TitleLocId()))));

        var administrator = shell.Player?.Name ?? "SERVER";
        AdminLog.Add(LogType.AdminCommands, LogImpact.Medium,
            $"{administrator} {operation} Civic Points for account {change.Character.AccountId}, character {change.Character.Name} (ID {change.Character.Id.Value}, slot {change.Character.Slot}), balance {change.PreviousPoints}->{change.NewPoints}, delta {change.Delta}. Reason: {reason}");
    }

    protected CompletionResult CompleteMutation(string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(
                Players.Sessions.Select(session => session.Name),
                Loc.GetString("cmd-civicpoints-hint-player")),
            2 => CompletionResult.FromHint(Loc.GetString("cmd-civicpoints-hint-slot")),
            3 => CompletionResult.FromHint(Loc.GetString("cmd-civicpoints-hint-amount")),
            4 => CompletionResult.FromHint(Loc.GetString("cmd-civicpoints-hint-reason")),
            _ => CompletionResult.Empty,
        };
    }
}

[AdminCommand(AdminFlags.Admin)]
public sealed class CivicPointsGetCommand : CivicPointsCommandBase
{
    public override string Command => "civicpoints_get";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError(Help);
            return;
        }

        var character = await ResolveCharacter(shell, args[0], args[1]);
        if (character == null)
            return;

        var points = await CivicPoints.GetPointsAsync(character.Id);
        if (points == null)
        {
            shell.WriteError(Loc.GetString("cmd-civicpoints-character-removed"));
            return;
        }

        WritePoints(shell, character, points.Value);
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(
                Players.Sessions.Select(session => session.Name),
                Loc.GetString("cmd-civicpoints-hint-player")),
            2 => CompletionResult.FromHint(Loc.GetString("cmd-civicpoints-hint-slot")),
            _ => CompletionResult.Empty,
        };
    }
}

public abstract class CivicPointsMutationCommand : CivicPointsCommandBase
{
    protected abstract string Operation { get; }
    protected virtual bool AllowNegative => false;

    protected abstract Task<CivicPointsChange?> Mutate(
        CharacterId character,
        long amount,
        CivicPointsSource source);

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 4)
        {
            shell.WriteError(Help);
            return;
        }

        var character = await ResolveCharacter(shell, args[0], args[1]);
        if (character == null)
            return;

        var validAmount = long.TryParse(args[2], out var amount) && (AllowNegative || amount > 0);
        if (!validAmount)
        {
            shell.WriteError(Loc.GetString("cmd-civicpoints-invalid-amount", ("amount", args[2])));
            return;
        }

        var reason = string.Join(' ', args.Skip(3)).Trim();
        if (string.IsNullOrWhiteSpace(reason))
        {
            shell.WriteError(Loc.GetString("cmd-civicpoints-reason-required"));
            return;
        }

        try
        {
            var change = await Mutate(character.Id, amount, CivicPointsSource.Admin(reason));
            if (change == null)
            {
                shell.WriteError(Loc.GetString("cmd-civicpoints-character-removed"));
                return;
            }

            WriteChange(shell, change, Operation, reason);
        }
        catch (OverflowException)
        {
            shell.WriteError(Loc.GetString("cmd-civicpoints-overflow"));
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return CompleteMutation(args);
    }
}

[AdminCommand(AdminFlags.Admin)]
public sealed class CivicPointsAddCommand : CivicPointsMutationCommand
{
    public override string Command => "civicpoints_add";
    protected override string Operation => "added";

    protected override Task<CivicPointsChange?> Mutate(
        CharacterId character,
        long amount,
        CivicPointsSource source)
    {
        return CivicPoints.AddPointsAsync(character, amount, source);
    }
}

[AdminCommand(AdminFlags.Admin)]
public sealed class CivicPointsRemoveCommand : CivicPointsMutationCommand
{
    public override string Command => "civicpoints_remove";
    protected override string Operation => "removed";

    protected override Task<CivicPointsChange?> Mutate(
        CharacterId character,
        long amount,
        CivicPointsSource source)
    {
        return CivicPoints.RemovePointsAsync(character, amount, source);
    }
}

[AdminCommand(AdminFlags.Admin)]
public sealed class CivicPointsSetCommand : CivicPointsMutationCommand
{
    public override string Command => "civicpoints_set";
    protected override string Operation => "set";
    protected override bool AllowNegative => true;

    protected override Task<CivicPointsChange?> Mutate(
        CharacterId character,
        long amount,
        CivicPointsSource source)
    {
        return CivicPoints.SetPointsAsync(character, amount, source);
    }
}
