using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Server._Misfits.Characters;
using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Shared._Misfits.Experience;
using Content.Shared.Administration;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Localization;

namespace Content.Server._Misfits.Experience.Commands;

public abstract partial class CharacterExperienceCommandBase : LocalizedCommands
{
    [Dependency] protected IAdminLogManager AdminLog = default!;
    [Dependency] protected ICharacterExperienceManager Experience = default!;
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
            shell.WriteError(Loc.GetString("cmd-characterxp-player-not-found", ("player", player)));
            return null;
        }

        if (!int.TryParse(slotText, out var slot) || slot < 0)
        {
            shell.WriteError(Loc.GetString("cmd-characterxp-invalid-slot", ("slot", slotText)));
            return null;
        }

        var characters = await Experience.GetCharactersAsync(playerData.UserId);
        var character = characters.SingleOrDefault(candidate => candidate.Slot == slot);
        if (character == null)
        {
            shell.WriteError(Loc.GetString("cmd-characterxp-character-not-found",
                ("player", player),
                ("slot", slot)));
        }

        return character;
    }

    protected bool TryParseGroup(IConsoleShell shell, string value, out ExperienceGroup group)
    {
        if (ExperienceGroupExtensions.TryParse(value, out group))
            return true;

        shell.WriteError(Loc.GetString("cmd-characterxp-invalid-group", ("group", value)));
        return false;
    }

    protected static bool TryParseAmount(
        IConsoleShell shell,
        ILocalizationManager localization,
        string value,
        bool allowZero,
        out long amount)
    {
        var valid = long.TryParse(value, out amount) && (allowZero ? amount >= 0 : amount > 0);
        if (valid)
            return true;

        shell.WriteError(localization.GetString("cmd-characterxp-invalid-amount", ("amount", value)));
        return false;
    }

    protected void WriteExperience(IConsoleShell shell, ExperienceGroup group, long totalExperience)
    {
        var progress = ExperienceMath.GetProgress(totalExperience);
        shell.WriteLine(Loc.GetString("cmd-characterxp-value",
            ("group", group.Id()),
            ("experience", totalExperience),
            ("level", progress.Level),
            ("progress", progress.CurrentLevelExperience),
            ("required", progress.ExperienceToNextLevel)));
    }

    protected void WriteChange(
        IConsoleShell shell,
        CharacterExperienceChange change,
        string operation,
        long requestedAmount,
        string reason)
    {
        shell.WriteLine(Loc.GetString("cmd-characterxp-change-success",
            ("operation", operation),
            ("character", change.Character.Name),
            ("characterId", change.Character.Id.Value),
            ("group", change.Group.Id()),
            ("oldExperience", change.PreviousExperience),
            ("newExperience", change.NewExperience),
            ("oldLevel", change.PreviousLevel),
            ("newLevel", change.NewLevel)));

        var administrator = shell.Player?.Name ?? "SERVER";
        AdminLog.Add(LogType.AdminCommands, LogImpact.Medium,
            $"{administrator} {operation} character experience for account {change.Character.AccountId}, character {change.Character.Name} (ID {change.Character.Id.Value}, slot {change.Character.Slot}), group {change.Group.Id()}, requested {requestedAmount}, XP {change.PreviousExperience}->{change.NewExperience}, level {change.PreviousLevel}->{change.NewLevel}. Reason: {reason}");
    }

    protected CompletionResult CompleteMutation(string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(
                Players.Sessions.Select(session => session.Name),
                Loc.GetString("cmd-characterxp-hint-player")),
            2 => CompletionResult.FromHint(Loc.GetString("cmd-characterxp-hint-slot")),
            3 => CompletionResult.FromHintOptions(
                ExperienceGroupExtensions.All.Select(group => group.Id()),
                Loc.GetString("cmd-characterxp-hint-group")),
            4 => CompletionResult.FromHint(Loc.GetString("cmd-characterxp-hint-amount")),
            5 => CompletionResult.FromHint(Loc.GetString("cmd-characterxp-hint-reason")),
            _ => CompletionResult.Empty,
        };
    }
}

[AdminCommand(AdminFlags.Admin)]
public sealed class CharacterExperienceListCommand : CharacterExperienceCommandBase
{
    public override string Command => "characterxp_list";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Help);
            return;
        }

        var playerData = await PlayerLocator.LookupIdByNameOrIdAsync(args[0]);
        if (playerData == null)
        {
            shell.WriteError(Loc.GetString("cmd-characterxp-player-not-found", ("player", args[0])));
            return;
        }

        var characters = await Experience.GetCharactersAsync(playerData.UserId);
        if (characters.Count == 0)
        {
            shell.WriteLine(Loc.GetString("cmd-characterxp-no-characters", ("player", args[0])));
            return;
        }

        shell.WriteLine(Loc.GetString("cmd-characterxp-list-header",
            ("player", args[0]),
            ("account", playerData.UserId)));
        foreach (var character in characters)
        {
            shell.WriteLine(Loc.GetString("cmd-characterxp-list-entry",
                ("slot", character.Slot),
                ("characterId", character.Id.Value),
                ("character", character.Name),
                ("selected", character.Selected ? " *" : string.Empty)));
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length == 1
            ? CompletionResult.FromHintOptions(
                Players.Sessions.Select(session => session.Name),
                Loc.GetString("cmd-characterxp-hint-player"))
            : CompletionResult.Empty;
    }
}

[AdminCommand(AdminFlags.Admin)]
public sealed class CharacterExperienceGetCommand : CharacterExperienceCommandBase
{
    public override string Command => "characterxp_get";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length is not (2 or 3))
        {
            shell.WriteError(Help);
            return;
        }

        var character = await ResolveCharacter(shell, args[0], args[1]);
        if (character == null)
            return;

        var values = await Experience.GetExperienceAsync(character.Id);
        if (values == null)
        {
            shell.WriteError(Loc.GetString("cmd-characterxp-character-removed"));
            return;
        }

        shell.WriteLine(Loc.GetString("cmd-characterxp-get-header",
            ("character", character.Name),
            ("characterId", character.Id.Value),
            ("slot", character.Slot)));

        if (args.Length == 3)
        {
            if (TryParseGroup(shell, args[2], out var group))
                WriteExperience(shell, group, values[group]);
            return;
        }

        foreach (var group in ExperienceGroupExtensions.All)
        {
            WriteExperience(shell, group, values[group]);
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(
                Players.Sessions.Select(session => session.Name),
                Loc.GetString("cmd-characterxp-hint-player")),
            2 => CompletionResult.FromHint(Loc.GetString("cmd-characterxp-hint-slot")),
            3 => CompletionResult.FromHintOptions(
                ExperienceGroupExtensions.All.Select(group => group.Id()),
                Loc.GetString("cmd-characterxp-hint-group-optional")),
            _ => CompletionResult.Empty,
        };
    }
}

public abstract class CharacterExperienceMutationCommand : CharacterExperienceCommandBase
{
    protected abstract string Operation { get; }

    protected abstract Task<CharacterExperienceChange?> Mutate(
        CharacterId character,
        ExperienceGroup group,
        long amount);

    protected virtual bool AllowZero => false;

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 5)
        {
            shell.WriteError(Help);
            return;
        }

        var character = await ResolveCharacter(shell, args[0], args[1]);
        if (character == null ||
            !TryParseGroup(shell, args[2], out var group) ||
            !TryParseAmount(shell, Loc, args[3], AllowZero, out var amount))
        {
            return;
        }

        var reason = string.Join(' ', args.Skip(4)).Trim();
        if (string.IsNullOrWhiteSpace(reason))
        {
            shell.WriteError(Loc.GetString("cmd-characterxp-reason-required"));
            return;
        }

        try
        {
            var change = await Mutate(character.Id, group, amount);
            if (change == null)
            {
                shell.WriteError(Loc.GetString("cmd-characterxp-character-removed"));
                return;
            }

            WriteChange(shell, change, Operation, amount, reason);
        }
        catch (OverflowException)
        {
            shell.WriteError(Loc.GetString("cmd-characterxp-overflow"));
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return CompleteMutation(args);
    }
}

[AdminCommand(AdminFlags.Admin)]
public sealed class CharacterExperienceSetCommand : CharacterExperienceMutationCommand
{
    public override string Command => "characterxp_set";
    protected override string Operation => "set";
    protected override bool AllowZero => true;

    protected override Task<CharacterExperienceChange?> Mutate(
        CharacterId character,
        ExperienceGroup group,
        long amount)
    {
        return Experience.SetExperienceAsync(character, group, amount);
    }
}

[AdminCommand(AdminFlags.Admin)]
public sealed class CharacterExperienceAddCommand : CharacterExperienceMutationCommand
{
    public override string Command => "characterxp_add";
    protected override string Operation => "added";

    protected override Task<CharacterExperienceChange?> Mutate(
        CharacterId character,
        ExperienceGroup group,
        long amount)
    {
        return Experience.AddExperienceAsync(character, group, amount);
    }
}

[AdminCommand(AdminFlags.Admin)]
public sealed class CharacterExperienceRemoveCommand : CharacterExperienceMutationCommand
{
    public override string Command => "characterxp_remove";
    protected override string Operation => "removed";

    protected override Task<CharacterExperienceChange?> Mutate(
        CharacterId character,
        ExperienceGroup group,
        long amount)
    {
        return Experience.RemoveExperienceAsync(character, group, amount);
    }
}
