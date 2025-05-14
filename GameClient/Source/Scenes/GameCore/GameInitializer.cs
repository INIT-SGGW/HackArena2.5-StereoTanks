﻿using System;
using GameClient.GameSceneComponents;
using Microsoft.Xna.Framework;
using MonoRivUI;

namespace GameClient.Scenes.GameCore;

/// <summary>
/// Represents the game initializer.
/// </summary>
/// <param name="game">The game scene to initialize.</param>
internal class GameInitializer(Game game)
{
    /// <summary>
    /// Creates a grid component.
    /// </summary>
    /// <returns>The created grid component.</returns>
    public GridComponent CreateGridComponent()
    {
        var ratio16x9 = new Ratio(16, 9).ToFloat();
        Vector2 GetRelativeSize()
        {
            var screenRatio = new Ratio(ScreenController.Width, ScreenController.Height).ToFloat();
            return new Vector2(Math.Min(screenRatio / ratio16x9, 1) * 0.8f);
        }

        var grid = new GridComponent()
        {
            IsEnabled = false,
            Parent = game.BaseComponent,
            Transform =
            {
                Alignment = Alignment.Center,
                Ratio = new Ratio(1, 1),
                RelativeSize = GetRelativeSize(),
                RelativeOffset = new Vector2(0.0f, 0.03f),
            },
        };

        ScreenController.ScreenChanged += (s, e) =>
        {
            grid.Transform.RelativeSize = GetRelativeSize();
        };

        return grid;
    }

#if STEREO

    /// <summary>
    /// Creates team bar panels.
    /// </summary>
    /// <returns>A tuple containing the two team bar panels.</returns>
    public List<TeamBarPanel> CreateTeamBarPanels()
    {
        var boxRelativeSize = new Vector2(0.22f, 1.0f);
        var boxRelativePadding = new Vector4(0.1f);
        var boxRelativeOffset = new Vector2(0.015f, 0.3f);
        var boxSpacing = 15;
        var boxElementsAlignment = Alignment.Top;

        var leftBox = new TeamBarPanel()
        {
            Parent = game.BaseComponent,
            ElementsAlignment = boxElementsAlignment,
            Spacing = boxSpacing,
            Transform =
            {
                Alignment = Alignment.Left,
                RelativeSize = boxRelativeSize,
                RelativeOffset = boxRelativeOffset,
            },
        };

        var rightBox = new TeamBarPanel()
        {
            Parent = game.BaseComponent,
            ElementsAlignment = boxElementsAlignment,
            Spacing = boxSpacing,
            Transform =
            {
                Alignment = Alignment.Right,
                RelativeSize = boxRelativeSize,
                RelativeOffset = boxRelativeOffset * new Vector2(-1f, 1f),
            },
        };

        leftBox.ContentContainer.Transform.RelativePadding = boxRelativePadding;
        rightBox.ContentContainer.Transform.RelativePadding = boxRelativePadding;
        return [leftBox, rightBox];
    }

#else

    /// <summary>
    /// Creates player bar panels.
    /// </summary>
    /// <returns>
    /// A tuple containing the player identity bar panel
    /// and the player stats bar panel.
    /// </returns>
    public Tuple<PlayerBarPanel<PlayerIdentityBar>, PlayerBarPanel<PlayerStatsBar>> CreatePlayerBarPanels()
    {
        var boxRelativeSize = new Vector2(0.22f, 1.0f);
        var boxRelativePadding = new Vector4(0.1f);
        var boxRelativeOffset = new Vector2(0.015f, 0.05f);
        var boxSpacing = 8;
        var boxElementsAlignment = Alignment.Center;

        var identityBox = new PlayerBarPanel<PlayerIdentityBar>()
        {
            Parent = game.BaseComponent,
            ElementsAlignment = boxElementsAlignment,
            Spacing = boxSpacing,
            Transform =
            {
                Alignment = Alignment.Left,
                RelativeSize = boxRelativeSize,
                RelativeOffset = boxRelativeOffset,
            },
        };

        var statsBox = new PlayerBarPanel<PlayerStatsBar>()
        {
            Parent = game.BaseComponent,
            ElementsAlignment = boxElementsAlignment,
            Spacing = boxSpacing,
            Transform =
            {
                Alignment = Alignment.Right,
                RelativeSize = boxRelativeSize,
                RelativeOffset = boxRelativeOffset * new Vector2(-1f, 1f),
            },
        };

        identityBox.ContentContainer.Transform.RelativePadding = boxRelativePadding;
        statsBox.ContentContainer.Transform.RelativePadding = boxRelativePadding;

        return new(identityBox, statsBox);
    }

#endif

    /// <summary>
    /// Creates a timer.
    /// </summary>
    /// <returns>The created timer.</returns>
    public Timer CreateTimer()
    {
        return new Timer()
        {
            Parent = game.BaseComponent,
            Transform =
            {
                Alignment = Alignment.TopLeft,
                RelativeSize = new Vector2(0.1f, 0.045f),
                RelativeOffset = new Vector2(0.03f, 0.06f),
            },
        };
    }

#if HACKATHON

    /// <summary>
    /// Creates a match name.
    /// </summary>
    /// <returns>The created match name.</returns>
    public Text CreateMatchName()
    {
        var font = new ScalableFont(Styles.Fonts.Paths.Main, 22)
        {
            AutoResize = true,
            Spacing = 10,
        };

        return new Text(font, Color.White)
        {
            Parent = game.BaseComponent,
            Value = "Match Name",
            Case = TextCase.Upper,
            AdjustTransformSizeToText = AdjustSizeOption.HeightAndWidth,
            Transform =
            {
                Alignment = Alignment.Top,
                RelativeOffset = new Vector2(0.0f, 0.04f),
            },
        };
    }

#endif

    /// <summary>
    /// Creates a menu button.
    /// </summary>
    /// <returns>The created menu button.</returns>
    public Button<Container> CreateMenuButton()
    {
        var button = new Button<Container>(new Container())
        {
            Parent = game.BaseComponent,
            Transform =
            {
                Alignment = Alignment.TopRight,
                RelativeOffset = new Vector2(-0.03f, 0.06f),
                RelativeSize = new Vector2(0.1f, 0.045f),
            },
        };

        var text = new LocalizedString("Buttons.Menu");
        var iconPath = "Images/Icons/menu.svg";
        var style = Styles.UI.GetButtonStyleWithIcon(text, iconPath, Alignment.Right);
        button.ApplyStyle(style);

        var options = new OverlayShowOptions(BlockFocusOnUnderlyingScenes: true);
        button.Clicked += (s, e) => Scene.ShowOverlay<GameOverlays.GameMenu>(options);

        return button;
    }
}
