﻿using Microsoft.Xna.Framework;
 using NFMWorld.Reactor;
 using NFMWorldLibrary.Backend.Gamemodes;
using WorldXaml.UI.Yoga;

namespace NFMWorld.DriverInterface.UI;

public partial class GarageDynamicStatBar : Node
{
    private const float MaxSpeed = 1000f;
    private const float SpeedUp = 0.1f;
    private const int FullBar = 100;

    [Property]
    public int BarMaxWidth
    {
        get;
        set
        {
            field = value;
            Width = value;
        }
    } = 100;

    [Property]
    public int BarHeight
    {
        get;
        set
        {
            field = value;
            Height = value + 28;
        }
    } = 10;

    private float _currentValue = 0f;

    [Property]
    public float TargetValue { get; set; }

    private float _speed = SpeedUp;

    public string StatName { get; set; } = "Unknown Stat";

    private static readonly Color[] BarColors =
    [
        new(255, 0, 0),
        new(128, 128, 128),
        new(255, 128, 0),
        new(128, 128, 128),
        new(255, 255, 0),
        new(128, 128, 128),
        new(128, 255, 0),
        new(128, 128, 128),
        new(0, 255, 0),
        new(128, 128, 128),
        new(0, 255, 128),
        new(128, 128, 128),
        new(0, 255, 255),
        new(128, 128, 128),
        new(0, 128, 255),
        new(128, 128, 128),
        new(0, 0, 255),
        new(128, 128, 128),
        new(128, 0, 255),
        new(128, 128, 128),
        new(255, 0, 255),
        new(128, 128, 128),
        new(255, 0, 128),
        new(128, 128, 128),
    ];

    public GarageDynamicStatBar()
    {
        Width = BarMaxWidth;
        Height = BarHeight + 28;
    }

    protected override void GameTick()
    {
        _currentValue += _speed;
        _currentValue = Math.Min(TargetValue * 100f, _currentValue);

        _speed += SpeedUp;
        _speed = Math.Min(_speed, MaxSpeed);
    }

    private static int GetColor(int lim, int i)
    {
        return i < 0 ? i % lim + lim : i % lim;
    }

    [ClientOnly]
    protected override void RenderContent(Vector2 position, Vector2 size)
    {
        var x = (int)position.X;
        var y = (int)position.Y;
        
        var multiples = 0;
        var remaining = _currentValue;

        while (remaining > FullBar)
        {
            remaining -= FullBar;
            multiples++;
        }

        G.SetColor(new Color(0, 0, 0));
        G.SetFont(new Font(FontFamily.DroidSans, FontStyle.Bold, 20));
        G.DrawStringStroke(StatName, x, y - 5);
        G.SetColor(new Color(255, 255, 255));
        G.DrawString(StatName, x, y - 5);

        var baseBarColorStart = multiples > 0 ? BarColors[GetColor(BarColors.Length, multiples - 1)] : new Color(0, 0, 0, 0);
        var baseBarColorEnd = multiples > 0 ? BarColors[GetColor(BarColors.Length, multiples)] : new Color(0, 0, 0, 0);

        var barColorStart = BarColors[GetColor(BarColors.Length, multiples)];
        var barColorEnd = BarColors[GetColor(BarColors.Length, multiples + 1)];

        G.SetLinearGradient(x, y, BarMaxWidth, BarHeight, [baseBarColorStart, baseBarColorEnd], null);
        G.FillRect(x, y, BarMaxWidth, BarHeight);

        var barRatio = (int)(remaining / FullBar * 100);
        barRatio *= BarMaxWidth / FullBar;

        G.SetLinearGradient(x, y, BarMaxWidth, BarHeight, [barColorStart, barColorEnd], null);
        G.FillRect(x, y, barRatio, BarHeight);

        G.SetColor(new Color(255, 255, 255));
        G.SetFont(new Font(FontFamily.DroidSans, FontStyle.Bold, 12));
        G.DrawString(((int)_currentValue).ToString(), x + 5, y + BarHeight);
        
        DrawDividers(x, y);
    }

    // Draw the black thing that overlays the stat itself...
    [ClientOnly]
    private void DrawDividers(int x, int y)
    {
        G.SetColor(new Color(0, 0, 0));
        G.DrawLine(x, y + BarHeight, x + BarMaxWidth, y + BarHeight);
        G.DrawLine(x, y, x, y + BarHeight);
        G.DrawLine(x + BarMaxWidth, y, x + BarMaxWidth, y + BarHeight);
    }
}
