using System.Collections.Generic;
using Content.Client.Gameplay;
using Content.Shared._Misfits.Experience;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Network;

namespace Content.Client._Misfits.Experience.UI;

public sealed class CharacterExperienceUIController : UIController,
    IOnStateEntered<GameplayState>,
    IOnStateExited<GameplayState>
{
    [Dependency] private IClientNetManager _net = default!;

    private readonly HashSet<CharacterExperienceMenuButton> _buttons = new();
    private CharacterExperienceWindow? _window;
    private MsgCharacterExperienceUpdate? _lastSnapshot;

    public override void Initialize()
    {
        base.Initialize();
        _net.RegisterNetMessage<MsgCharacterExperienceUpdate>(HandleSnapshot);
        _net.RegisterNetMessage<MsgCharacterExperienceRequest>();
    }

    public void OnStateEntered(GameplayState state)
    {
        _window = UIManager.CreateWindow<CharacterExperienceWindow>();
        _window.OnClose += DeactivateButton;
        _window.OnOpen += ActivateButton;

        if (_lastSnapshot != null)
            ApplySnapshot(_lastSnapshot);
    }

    public void OnStateExited(GameplayState state)
    {
        _window?.Close();
        _window = null;
        _lastSnapshot = null;
        DeactivateButton();
    }

    public void RegisterButton(CharacterExperienceMenuButton button)
    {
        _buttons.Add(button);
        button.SetPressed(_window?.IsOpen == true);
    }

    public void UnregisterButton(CharacterExperienceMenuButton button)
    {
        _buttons.Remove(button);
    }

    public void ToggleWindow()
    {
        if (_window == null)
            return;

        if (_window.IsOpen)
        {
            _window.Close();
            return;
        }

        _net.ClientSendMessage(new MsgCharacterExperienceRequest());
        _window.OpenCenteredRight();
        _window.MoveToFront();
    }

    private void HandleSnapshot(MsgCharacterExperienceUpdate message)
    {
        _lastSnapshot = message;
        ApplySnapshot(message);
    }

    private void ApplySnapshot(MsgCharacterExperienceUpdate message)
    {
        _window?.UpdateSnapshot(message.HasCharacter, message.CharacterName, message.Experience);
    }

    private void DeactivateButton()
    {
        SetButtonsPressed(false);
    }

    private void ActivateButton()
    {
        SetButtonsPressed(true);
    }

    private void SetButtonsPressed(bool pressed)
    {
        foreach (var button in _buttons)
        {
            button.SetPressed(pressed);
        }
    }
}
