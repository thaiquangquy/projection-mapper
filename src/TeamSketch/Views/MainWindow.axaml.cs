using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Splat;
using TeamSketch.DependencyInjection;
using TeamSketch.Services;
using TeamSketch.Utils;
using TeamSketch.ViewModels;

namespace TeamSketch.Views;

public partial class MainWindow : Window
{
    private readonly IBrushService _brushService;
    private readonly IRenderer _renderer;
    private readonly ISignalRService _signalRService;

    private Point currentPoint = new();
    private bool pressed;

    public MainWindow()
    {
        InitializeComponent();

        _brushService = Locator.Current.GetRequiredService<IBrushService>();
        _renderer = new Renderer(_brushService, canvas);

        _signalRService = Locator.Current.GetRequiredService<ISignalRService>();
        _signalRService.DrewPoint += SignalRService_DrewPoint;
        _signalRService.DrewLine += SignalRService_DrewLine;

        canvas.PointerMoved += ThrottleHelper.CreateThrottledEventHandler(Canvas_PointerMoved, TimeSpan.FromMilliseconds(8));
    }

    private void SignalRService_DrewPoint(object sender, DrewEventArgs e)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            var point = PayloadConverter.BytesToPoint(e.Data);
            canvas.Children.Add(point);
        });
    }

    private void SignalRService_DrewLine(object sender, DrewEventArgs e)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            var point = PayloadConverter.BytesToLine(e.Data);
            canvas.Children.Add(point);
        });
    }

    private void Canvas_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        currentPoint = e.GetPosition(canvas);
        pressed = true;
    }

    private void Canvas_PointerReleased(object sender, PointerReleasedEventArgs e)
    {
        pressed = false;

        var (x, y) = _renderer.RestrictPointToCanvas(currentPoint.X, currentPoint.Y);
        _renderer.DrawPoint(x, y);

        try
        {
            _ = _signalRService.DrawPointAsync(currentPoint.X, currentPoint.Y, _brushService.Thickness, _brushService.Color);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex.Message);
        }
    }

    private void Canvas_PointerMoved(object sender, PointerEventArgs e)
    {
        if (!pressed)
        {
            return;
        }

        Point newPosition = e.GetPosition(canvas);
        var (x, y) = _renderer.RestrictPointToCanvas(newPosition.X, newPosition.Y);

        _renderer.DrawLine(currentPoint.X, currentPoint.Y, x, y);

        currentPoint = new Point(x, y);

        try
        {
            _ = _signalRService.DrawLineAsync(currentPoint.X, currentPoint.Y, newPosition.X, newPosition.Y, _brushService.Thickness, _brushService.Color);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex.Message);
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        var vm = DataContext as MainWindowViewModel;
        _ = vm.Disconnect();

        var window = new EnterWindow
        {
            DataContext = new EnterViewModel(),
            Topmost = true,
            CanResize = false
        };
        window.Show();
        window.Activate();
    }
}
