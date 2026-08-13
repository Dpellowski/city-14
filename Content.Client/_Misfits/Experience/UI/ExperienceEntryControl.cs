using System;
using Content.Shared._Misfits.Experience;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Misfits.Experience.UI;

public sealed class ExperienceEntryControl : PanelContainer
{
    private static readonly Color Amber = Color.FromHex("#E6A33A");
    private static readonly Color AmberBright = Color.FromHex("#FFC15A");
    private static readonly Color DimText = Color.FromHex("#9B946F");
    private static readonly Color PanelBackground = Color.FromHex("#151A17E8");
    private static readonly Color BarBackground = Color.FromHex("#292D23");

    private readonly Label _levelLabel;
    private readonly Label _progressLabel;
    private readonly Label _totalLabel;
    private readonly ProgressBar _progressBar;

    public ExperienceGroup Group { get; }

    public ExperienceEntryControl(ExperienceGroup group)
    {
        Group = group;
        HorizontalExpand = true;
        Margin = new Thickness(4, 3);
        PanelOverride = new StyleBoxFlat
        {
            BackgroundColor = PanelBackground,
            BorderColor = Color.FromHex("#5E5938"),
            BorderThickness = new Thickness(1),
        };

        var content = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            Margin = new Thickness(10, 7),
            SeparationOverride = 3,
        };
        AddChild(content);

        var heading = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true,
        };
        content.AddChild(heading);

        heading.AddChild(new Label
        {
            Text = Loc.GetString($"misfits-experience-group-{group.Id()}"),
            FontColorOverride = AmberBright,
            HorizontalExpand = true,
        });

        _levelLabel = new Label
        {
            FontColorOverride = Amber,
            HorizontalAlignment = HAlignment.Right,
        };
        heading.AddChild(_levelLabel);

        _progressBar = new ProgressBar
        {
            HorizontalExpand = true,
            MinValue = 0,
            MaxValue = 1,
            SetHeight = 22,
            BackgroundStyleBoxOverride = new StyleBoxFlat
            {
                BackgroundColor = BarBackground,
                BorderColor = Color.FromHex("#626849"),
                BorderThickness = new Thickness(1),
            },
            ForegroundStyleBoxOverride = new StyleBoxFlat
            {
                BackgroundColor = Amber,
                BorderColor = AmberBright,
                BorderThickness = new Thickness(1, 0, 1, 0),
            },
        };
        content.AddChild(_progressBar);

        _progressLabel = new Label
        {
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center,
            FontColorOverride = Color.FromHex("#F3E5C0"),
        };
        _progressBar.AddChild(_progressLabel);

        _totalLabel = new Label
        {
            HorizontalAlignment = HAlignment.Right,
            FontColorOverride = DimText,
        };
        content.AddChild(_totalLabel);

        UpdateExperience(0);
    }

    public void UpdateExperience(long totalExperience)
    {
        var progress = ExperienceMath.GetProgress(totalExperience);
        _levelLabel.Text = Loc.GetString("misfits-experience-level", ("level", progress.Level));
        _progressLabel.Text = Loc.GetString("misfits-experience-progress",
            ("current", progress.CurrentLevelExperience),
            ("required", progress.ExperienceToNextLevel));
        _totalLabel.Text = Loc.GetString("misfits-experience-total", ("total", totalExperience));
        _progressBar.Value = Math.Clamp(progress.Fraction, 0f, 1f);
        ToolTip = $"{totalExperience} XP";
    }
}
