using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CopilotTrayStats.Models;
using CopilotTrayStats.Services;
using CopilotTrayStats.ViewModels;
using CopilotTrayStats.Views;
using H.NotifyIcon;
using Color = System.Windows.Media.Color;

namespace CopilotTrayStats;

public partial class App : Application
{
    private TaskbarIcon? _trayIcon;
    private MainWindow? _popup;
    private MainViewModel? _viewModel;
    private SettingsViewModel? _settingsViewModel;
    private UsageHistoryViewModel? _usageHistoryViewModel;
    private CopilotApiService? _apiService;
    private UsageHistoryService? _historyService;
    private UpdateService? _updateService;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        GitHubAuthService authService = new();
        _apiService = new CopilotApiService(authService);
        CopilotApiService apiService = _apiService;

        SettingsService settingsService = new();
        AppSettings settings = settingsService.Load();

        _viewModel = new MainViewModel(apiService);
        _viewModel.SetRefreshInterval(settings.RefreshIntervalMinutes);
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;

        // Restore last-known values immediately so the UI isn't blank on first open
        CachedState? cached = settingsService.LoadState();
        if (cached is not null)
            _viewModel.ApplyCachedState(cached);

        _historyService = new UsageHistoryService();
        UsageHistoryService historyService = _historyService;

        _viewModel.OnSuccessfulRefresh = state =>
        {
            settingsService.SaveState(state);
            historyService.RecordEntry(new DailyUsageEntry
            {
                Date              = DateOnly.FromDateTime(DateTime.Today),
                PremiumRemaining  = state.PremiumRemaining,
                PremiumTotal      = state.PremiumTotal,
                QuotaResetDateUtc = state.QuotaResetDateUtc,
            });
        };

        _updateService = new UpdateService(authService);
        _settingsViewModel = new SettingsViewModel(settingsService, _updateService);
        _viewModel.ShowUsedRequests = settings.ShowUsedRequests;
        _settingsViewModel.RefreshIntervalChanged += mins => _viewModel.SetRefreshInterval(mins);
        _settingsViewModel.ShowUsedRequestsChanged += show => _viewModel!.ShowUsedRequests = show;

        _usageHistoryViewModel = new UsageHistoryViewModel(historyService);

        _popup = new MainWindow
        {
            DataContext = _viewModel,
            SettingsViewModel = _settingsViewModel,
            UsageHistoryViewModel = _usageHistoryViewModel,
        };

        _trayIcon = new TaskbarIcon
        {
            ToolTipText = _viewModel.TooltipText,
            Icon = CreateCopilotIcon(Colors.Gray),
            LeftClickCommand = new RelayCommand(TogglePopup),
            DoubleClickCommand = new RelayCommand(TogglePopup)
        };

        // Context menu
        ContextMenu menu = new();

        MenuItem refreshItem = new() { Header = "Refresh" };
        refreshItem.Click += (_, _) => _viewModel.RefreshCommand.Execute(null);
        menu.Items.Add(refreshItem);

        menu.Items.Add(new Separator());

        MenuItem exitItem = new() { Header = "Exit" };
        exitItem.Click += (_, _) => ExitApp();
        menu.Items.Add(exitItem);

        _trayIcon.ContextMenu = menu;

        // Register the icon with the Windows shell (required when created in code, not XAML)
        _trayIcon.ForceCreate();

        // Initial data load
        await _viewModel.RefreshCommand.ExecuteAsync(null);
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (_trayIcon is null || _viewModel is null) return;

        if (e.PropertyName is nameof(MainViewModel.TooltipText))
            _trayIcon.ToolTipText = _viewModel.TooltipText;

        if (e.PropertyName is nameof(MainViewModel.UsageLevel))
        {
            Color color = _viewModel.UsageLevel switch
            {
                UsageLevel.Good     => Color.FromRgb(46, 160, 67),
                UsageLevel.Warning  => Color.FromRgb(210, 153, 34),
                UsageLevel.Critical => Color.FromRgb(218, 54, 51),
                _                   => Colors.Gray
            };
            _trayIcon.Icon = CreateCopilotIcon(color);
        }
    }

    private void TogglePopup()
    {
        if (_popup is null) return;

        if (_popup.IsVisible)
        {
            _popup.Hide();
        }
        else
        {
            _popup.Show();
            _popup.PositionNearTray();
            _popup.Activate();

            // Refresh if data is older than the configured interval
            if (_viewModel is not null && _settingsViewModel is not null
                && _viewModel.IsStale(_settingsViewModel.SelectedRefreshOption.Minutes))
            {
                _ = _viewModel.RefreshCommand.ExecuteAsync(null);
            }
        }
    }

    private void ExitApp()
    {
        _trayIcon?.Dispose();
        _apiService?.Dispose();
        _updateService?.Dispose();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        _apiService?.Dispose();
        _updateService?.Dispose();
        base.OnExit(e);
    }

    // GitHub Copilot SVG path (viewBox 0 0 24 24, fill-rule evenodd)
    private const string CopilotSvgPath =
        "M19.245 5.364c1.322 1.36 1.877 3.216 2.11 5.817.622 0 1.2.135 1.592.654l.73.964c.21.278.323.61.323.955v2.62" +
        "c0 .339-.173.669-.453.868C20.239 19.602 16.157 21.5 12 21.5c-4.6 0-9.205-2.583-11.547-4.258-.28-.2-.452-.53-.453-.868" +
        "v-2.62c0-.345.113-.679.321-.956l.73-.963c.392-.517.974-.654 1.593-.654l.029-.297c.25-2.446.81-4.213 2.082-5.52" +
        " 2.461-2.54 5.71-2.851 7.146-2.864h.198c1.436.013 4.685.323 7.146 2.864zm-7.244 4.328c-.284 0-.613.016-.962.05" +
        "-.123.447-.305.85-.57 1.108-1.05 1.023-2.316 1.18-2.994 1.18-.638 0-1.306-.13-1.851-.464-.516.165-1.012.403-1.044.996" +
        "a65.882 65.882 0 00-.063 2.884l-.002.48c-.002.563-.005 1.126-.013 1.69.002.326.204.63.51.765 2.482 1.102 4.83 1.657" +
        " 6.99 1.657 2.156 0 4.504-.555 6.985-1.657a.854.854 0 00.51-.766c.03-1.682.006-3.372-.076-5.053-.031-.596-.528-.83" +
        "-1.046-.996-.546.333-1.212.464-1.85.464-.677 0-1.942-.157-2.993-1.18-.266-.258-.447-.661-.57-1.108-.32-.032-.64-.049-.96-.05" +
        "zm-2.525 4.013c.539 0 .976.426.976.95v1.753c0 .525-.437.95-.976.95a.964.964 0 01-.976-.95v-1.752c0-.525.437-.951.976-.951" +
        "zm5 0c.539 0 .976.426.976.95v1.753c0 .525-.437.95-.976.95a.964.964 0 01-.976-.95v-1.752c0-.525.437-.951.976-.951" +
        "zM7.635 5.087c-1.05.102-1.935.438-2.385.906-.975 1.037-.765 3.668-.21 4.224.405.394 1.17.657 1.995.657h.09" +
        "c.649-.013 1.785-.176 2.73-1.11.435-.41.705-1.433.675-2.47-.03-.834-.27-1.52-.63-1.813-.39-.336-1.275-.482-2.265-.394" +
        "zm6.465.394c-.36.292-.6.98-.63 1.813-.03 1.037.24 2.06.675 2.47.968.957 2.136 1.104 2.776 1.11h.044" +
        "c.825 0 1.59-.263 1.995-.657.555-.556.765-3.187-.21-4.224-.45-.468-1.335-.804-2.385-.906-.99-.088-1.875.058-2.265.394" +
        "zM12 7.615c-.24 0-.525.015-.84.044.03.16.045.336.06.526l-.001.159a2.94 2.94 0 01-.014.25" +
        "c.225-.022.425-.027.612-.028h.366c.187 0 .387.006.612.028-.015-.146-.015-.277-.015-.409.015-.19.03-.365.06-.526" +
        "a9.29 9.29 0 00-.84-.044z";

    private static Icon CreateCopilotIcon(Color fill)
    {
        const int size = 32;
        const double viewBox = 24.0;

        Geometry rawGeometry = Geometry.Parse(CopilotSvgPath);
        PathGeometry pathGeometry = PathGeometry.CreateFromGeometry(rawGeometry);
        pathGeometry.FillRule = FillRule.EvenOdd;

        SolidColorBrush brush = new(fill);
        brush.Freeze();

        DrawingVisual visual = new();
        using (DrawingContext dc = visual.RenderOpen())
        {
            double scale = size / viewBox;
            dc.PushTransform(new ScaleTransform(scale, scale));
            dc.DrawGeometry(brush, null, pathGeometry);
            dc.Pop();
        }

        RenderTargetBitmap rtb = new(size, size, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(visual);

        PngBitmapEncoder encoder = new();
        encoder.Frames.Add(BitmapFrame.Create(rtb));
        using MemoryStream ms = new();
        encoder.Save(ms);
        ms.Position = 0;

        using Bitmap gdiBmp = new(ms);
        return System.Drawing.Icon.FromHandle(gdiBmp.GetHicon());
    }

    private sealed class RelayCommand(Action execute) : System.Windows.Input.ICommand
    {
#pragma warning disable CS0067
        public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067
        public bool CanExecute(object? _) => true;
        public void Execute(object? _) => execute();
    }
}
