using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace TeamSketch.Models;

public class BrushSettings
{
    private static readonly Dictionary<ColorsEnum, SolidColorBrush> ColorLookup = new()
    {
        { ColorsEnum.Default, new SolidColorBrush(Color.FromRgb(34, 34, 34)) },
        { ColorsEnum.Eraser, new SolidColorBrush(Color.FromRgb(255, 255, 255)) },
        { ColorsEnum.Red, new SolidColorBrush(Color.FromRgb(235, 51, 36)) },
        { ColorsEnum.Blue, new SolidColorBrush(Color.FromRgb(0, 162, 232)) },
        { ColorsEnum.Green, new SolidColorBrush(Color.FromRgb(34, 177, 76)) },
        { ColorsEnum.Yellow, new SolidColorBrush(Color.FromRgb(255, 242, 0)) },
        { ColorsEnum.Orange, new SolidColorBrush(Color.FromRgb(255, 127, 39)) },
        { ColorsEnum.Purple, new SolidColorBrush(Color.FromRgb(163, 73, 164)) },
        { ColorsEnum.Pink, new SolidColorBrush(Color.FromRgb(255, 174, 201)) },
        { ColorsEnum.Gray, new SolidColorBrush(Color.FromRgb(195, 195, 195)) }
    };
    private static readonly Dictionary<ThicknessEnum, double> ThicknessLookup = new()
    {
        { ThicknessEnum.Thin, 2 },
        { ThicknessEnum.SemiThin, 4 },
        { ThicknessEnum.Medium, 6 },
        { ThicknessEnum.SemiThick, 8 },
        { ThicknessEnum.Thick, 10 },
        { ThicknessEnum.Eraser, 50 }
    };
    private readonly IAssetLoader assetLoader;

    /// <param name="cursorsPath">Required parameter. Use empty string for unit testing.</param>
    /// <exception cref="ArgumentException"></exception>
    public BrushSettings()
    {
        assetLoader = AvaloniaLocator.Current.GetService<IAssetLoader>();

        BrushColor = ColorsEnum.Default;
        BrushThickness = ThicknessEnum.SemiThin;
    }

    public event EventHandler<BrushChangedEventArgs> BrushChanged;

    public Cursor Cursor { get; private set; }

    private ColorsEnum brushColor;
    public ColorsEnum BrushColor
    {
        get => brushColor;
        set
        {
            brushColor = value;
            ColorBrush = ColorLookup[value];

            BrushChanged?.Invoke(null, new BrushChangedEventArgs(Cursor));
        }
    }

    public SolidColorBrush ColorBrush { get; private set; }

    private ThicknessEnum brushThickness;
    public ThicknessEnum BrushThickness
    {
        get => brushThickness;
        set
        {
            brushThickness = value;
            Thickness = ThicknessLookup[value];
            HalfThickness = Thickness / 2;

            MaxBrushPointX = Globals.CanvasWidth - HalfThickness;
            MaxBrushPointY = Globals.CanvasHeight - HalfThickness;
            MinBrushPoint = HalfThickness;

            BrushChanged?.Invoke(null, new BrushChangedEventArgs(Cursor));
        }
    }

    public double Thickness { get; private set; }
    public double HalfThickness { get; private set; }

    public double MaxBrushPointX { get; private set; }
    public double MaxBrushPointY { get; private set; }
    public double MinBrushPoint { get; private set; }

    public static SolidColorBrush FindColorBrush(byte color)
    {
        return ColorLookup[(ColorsEnum)color];
    }

    public static double FindThickness(byte thickness)
    {
        return ThicknessLookup[(ThicknessEnum)thickness];
    }
}

public class BrushChangedEventArgs : EventArgs
{
    public BrushChangedEventArgs(Cursor cursor)
    {
        Cursor = cursor;
    }

    public Cursor Cursor { get; private set; }
}
