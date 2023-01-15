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

            var mainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel()
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
