using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Misfits.Experience.UI;

/// <summary>
/// Opens the persistent experience record from the top of the character menu.
/// </summary>
public sealed class CharacterExperienceMenuButton : Button
{
    private CharacterExperienceUIController? _controller;

    public CharacterExperienceMenuButton()
    {
        Text = Loc.GetString("misfits-experience-character-menu-button");
        ToolTip = Loc.GetString("misfits-experience-hud-tooltip");
        ToggleMode = true;
        HorizontalExpand = true;
        Margin = new Thickness(0, 6, 0, 8);
        MinHeight = 34;
        OnPressed += _ => _controller?.ToggleWindow();
    }

    protected override void EnteredTree()
    {
        base.EnteredTree();
        _controller = UserInterfaceManager.GetUIController<CharacterExperienceUIController>();
        _controller.RegisterButton(this);
    }

    protected override void ExitedTree()
    {
        _controller?.UnregisterButton(this);
        _controller = null;
        base.ExitedTree();
    }

    public void SetPressed(bool pressed)
    {
        Pressed = pressed;
    }
}
