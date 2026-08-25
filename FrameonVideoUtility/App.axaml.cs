using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using FrameonVideoUtility.Service.Logging;
using FrameonVideoUtility.ViewModels;
using FrameonVideoUtility.Views;

namespace FrameonVideoUtility
{
    public partial class App : Application
    {
        private static bool _sandboxCleanupAttempted;

        private AboutWindow? _aboutWindow;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
            ConfigureNativeApplicationMenu();
        }

        private void ConfigureNativeApplicationMenu()
        {
            var aboutMenuItem = new NativeMenuItem("About FrameOn...");
            aboutMenuItem.Click += AppAbout_OnClick;

            var quitMenuItem = new NativeMenuItem("Quit FrameOn");
            quitMenuItem.Click += (_, _) =>
            {
                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                    desktop.Shutdown();
            };

            var appMenu = new NativeMenu();
            appMenu.Add(aboutMenuItem);
            appMenu.Add(new NativeMenuItemSeparator());
            appMenu.Add(quitMenuItem);

            NativeMenu.SetMenu(this, appMenu);
        }

        private void AppAbout_OnClick(object? sender, EventArgs args)
        {
            if (_aboutWindow is { IsVisible: true })
            {
                _aboutWindow.Activate();
                return;
            }

            _aboutWindow = new AboutWindow();
            _aboutWindow.Closed += (_, _) => _aboutWindow = null;

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: { } owner })
            {
                _aboutWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                _aboutWindow.Show(owner);
            }
            else
            {
                _aboutWindow.Show();
            }

            AppLog.Info("About window opened from native application menu.");
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = new Window
                {
                    Title = "FrameOn",
                    Width = 1200,
                    Height = 620,
                    MinWidth = 1050,
                    MinHeight = 620,
                    CanResize = true,
                    Icon = LoadAppIcon(),
                    Content = new MainView
                    {
                        DataContext = new MainViewModel()
                    }
                };

                mainWindow.Closing += (_, _) =>
                {
                    CleanupSandboxOnce();
                };

                desktop.ShutdownRequested += (_, _) =>
                {
                    CleanupSandboxOnce();
                };

                desktop.Exit += (_, _) =>
                {
                    CleanupSandboxOnce();
                };

                AppDomain.CurrentDomain.ProcessExit += (_, _) =>
                {
                    CleanupSandboxOnce();
                };

                desktop.MainWindow = mainWindow;
            }
            else if (ApplicationLifetime is IActivityApplicationLifetime singleViewFactoryApplicationLifetime)
            {
                singleViewFactoryApplicationLifetime.MainViewFactory = () => new MainView
                {
                    DataContext = new MainViewModel()
                };
            }
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
            {
                singleViewPlatform.MainView = new MainView
                {
                    DataContext = new MainViewModel()
                };
            }

            base.OnFrameworkInitializationCompleted();
        }

        private static WindowIcon? LoadAppIcon()
        {
            try
            {
                var iconUri = new Uri("avares://FrameonVideoUtility/Assets/frameon_icon_windows.ico");
                using var iconStream = AssetLoader.Open(iconUri);

                return new WindowIcon(iconStream);
            }
            catch (Exception ex)
            {
                AppLog.Warning($"[Startup] Could not load app icon: {ex.Message}");
                return null;
            }
        }

        private static void CleanupSandboxOnce()
        {
            if (_sandboxCleanupAttempted)
                return;

            _sandboxCleanupAttempted = true;

            try
            {
                MainView.CleanupSandbox();
            }
            catch (Exception ex)
            {
                AppLog.Error($"[Shutdown] Sandbox cleanup failed: {ex.Message}");
            }
        }
    }
}
