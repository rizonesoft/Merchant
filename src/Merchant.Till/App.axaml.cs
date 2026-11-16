using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Merchant.Till.ViewModels;
using Merchant.Till.Views;

namespace Merchant.Till;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var runtime = TillRuntime.Open(TillRuntime.DefaultDataDir);
            var viewModel = new MainViewModel(runtime);
            var window = new MainWindow { DataContext = viewModel };
            viewModel.PickBackupToSave = window.PickBackupToSave;
            viewModel.PickBackupToOpen = window.PickBackupToOpen;
            desktop.MainWindow = window;

            // Selling never waits for the network: the till is usable before sync is up, and stays
            // usable if sync cannot start at all.
            _ = StartNetworkAsync(runtime, viewModel);
            desktop.Exit += (_, _) => runtime.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static async Task StartNetworkAsync(TillRuntime runtime, MainViewModel viewModel)
    {
        try
        {
            await Task.Run(runtime.StartNetworkAsync);
        }
        catch (Exception e)
        {
            Dispatcher.UIThread.Post(() => viewModel.SyncStatus = "Sync is off: " + e.Message);
        }
    }
}
