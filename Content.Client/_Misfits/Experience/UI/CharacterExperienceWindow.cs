using System.Collections.Generic;
using System.Numerics;
using Content.Client.UserInterface.Controls;
using Content.Shared._Misfits.Experience;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Misfits.Experience.UI;

public sealed class CharacterExperienceWindow : FancyWindow
{
    private static readonly Color Amber = Color.FromHex("#E6A33A");
    private static readonly Color AmberBright = Color.FromHex("#FFC15A");
    private static readonly Color DimText = Color.FromHex("#9B946F");

    private readonly Dictionary<ExperienceGroup, ExperienceEntryControl> _entries = new();
    private readonly Label _subtitle;
    private readonly Label _unavailable;

    public CharacterExperienceWindow()
    {
        Title = Loc.GetString("misfits-experience-window-title");
        MinSize = new Vector2(460, 420);
        SetSize = new Vector2(520, 590);

        var outerPanel = new PanelContainer
        {
            HorizontalExpand = true,
            VerticalExpand = true,
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = Color.FromHex("#101411F2"),
                BorderColor = Color.FromHex("#8F7435"),
                BorderThickness = new Thickness(2),
            },
        };
        ContentsContainer.AddChild(outerPanel);

        var content = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalExpand = true,
            Margin = new Thickness(12),
            SeparationOverride = 7,
        };
        outerPanel.AddChild(content);

        content.AddChild(new Label
        {
            Text = "// CIVIC PERSONNEL SYSTEM //",
            FontColorOverride = Amber,
        });

        _subtitle = new Label
        {
            FontColorOverride = AmberBright,
        };
        content.AddChild(_subtitle);

        content.AddChild(new PanelContainer
        {
            SetHeight = 2,
            PanelOverride = new StyleBoxFlat { BackgroundColor = Color.FromHex("#8F7435") },
        });

        _unavailable = new Label
        {
            Text = Loc.GetString("misfits-experience-unavailable"),
            FontColorOverride = DimText,
            HorizontalAlignment = HAlignment.Center,
            Margin = new Thickness(0, 12),
            Visible = false,
        };
        content.AddChild(_unavailable);

        var tabs = new TabContainer
        {
            HorizontalExpand = true,
            VerticalExpand = true,
        };
        content.AddChild(tabs);

        var faction = MakeTab(ExperienceGroupExtensions.Faction);
        var character = MakeTab(ExperienceGroupExtensions.Character);
        tabs.AddChild(faction);
        tabs.AddChild(character);
        TabContainer.SetTabTitle(faction, Loc.GetString("misfits-experience-faction-tab"));
        TabContainer.SetTabTitle(character, Loc.GetString("misfits-experience-character-tab"));
    }

    public void UpdateSnapshot(
        bool hasCharacter,
        string characterName,
        IReadOnlyDictionary<ExperienceGroup, long> experience)
    {
        _unavailable.Visible = !hasCharacter;
        _subtitle.Text = hasCharacter
            ? Loc.GetString("misfits-experience-window-subtitle", ("character", characterName))
            : Loc.GetString("misfits-experience-unavailable");

        foreach (var group in ExperienceGroupExtensions.All)
        {
            _entries[group].UpdateExperience(experience.GetValueOrDefault(group));
        }
    }

    private ScrollContainer MakeTab(IEnumerable<ExperienceGroup> groups)
    {
        var scroll = new ScrollContainer
        {
            HorizontalExpand = true,
            VerticalExpand = true,
            HScrollEnabled = false,
        };
        var list = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            Margin = new Thickness(4, 8),
        };
        scroll.AddChild(list);

        foreach (var group in groups)
        {
            var entry = new ExperienceEntryControl(group);
            _entries.Add(group, entry);
            list.AddChild(entry);
        }

        return scroll;
    }
}
