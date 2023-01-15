using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using TeamSketch.Services;
using TeamSketch.ViewModels;
using TeamSketch.Views;

namespace TeamSketch;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
        {
            desktopLifetime.Startup += Startup;

            //var window = new LobbyWindow
            //{
            //    DataContext = new LobbyViewModel(),
            //    Topmost = true,
            //    CanResize = false
            //};
            //window.Show();
            //window.Activate();

            SignalRService signalRService = new SignalRService(new AppState());
            var mainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(signalRService)
            };
            mainWindow.Show();
            mainWindow.Activate();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void Startup(object sender, ControlledApplicationLifetimeStartupEventArgs e)
    {
        if (e.Args.Length > 0)
        {
            Globals.RenderingIntervalMs = short.Parse(e.Args[0]);
        }
    }
}
