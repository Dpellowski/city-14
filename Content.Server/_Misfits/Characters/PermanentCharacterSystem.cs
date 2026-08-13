using Content.Shared.GameTicking;

namespace Content.Server._Misfits.Characters;

/// <summary>
/// Pins persistent progression operations to the profile used to spawn the current character.
/// </summary>
public sealed partial class PermanentCharacterSystem : EntitySystem
{
    [Dependency] private IPermanentCharacterManager _characters = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(_ => _characters.ClearActiveCharacters());
    }

    private async void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        await _characters.BindActiveCharacterAsync(args.Player);
    }
}
