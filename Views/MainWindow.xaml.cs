using System.Windows;
using System.Windows.Input;
using CopilotTrayStats.ViewModels;

namespace CopilotTrayStats.Views;

public partial class MainWindow : Window
{
    private int _copilotIconClickCount;

    public SettingsViewModel? SettingsViewModel { get; set; }
    public UsageHistoryViewModel? UsageHistoryViewModel { get; set; }

    public MainWindow()
    {
        InitializeComponent();
    }

    private void Window_Deactivated(object sender, EventArgs e)
    {
        _copilotIconClickCount = 0;
        HistoryPanel.Visibility = Visibility.Collapsed;
        Hide();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1)
            DragMove();
    }

    private void CopilotIcon_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true; // prevent bubbling to TitleBar DragMove
        _copilotIconClickCount++;
        if (_copilotIconClickCount >= 5)
        {
            _copilotIconClickCount = 0;
            if (DataContext is MainViewModel vm)
                vm.IsDebugVisible = !vm.IsDebugVisible;
        }
    }

    private void ChartButton_Click(object sender, RoutedEventArgs e)
    {
        if (HistoryPanel.Visibility == Visibility.Visible)
        {
            HistoryPanel.Visibility = Visibility.Collapsed;
            PositionNearTray();
            return;
        }

        if (UsageHistoryViewModel is null) return;
        UsageHistoryViewModel.Reload();

        BarsItemsControl.ItemsSource = UsageHistoryViewModel.Bars;
        if (UsageHistoryViewModel.Bars.Count == 0)
        {
            HistoryEmptyText.Visibility = Visibility.Visible;
            HistorySubtitle.Text = "Since last reset — no data recorded yet";
        }
        else
        {
            HistoryEmptyText.Visibility = Visibility.Collapsed;
            int total = UsageHistoryViewModel.TotalUsed;
            HistorySubtitle.Text = $"Since last reset — {total} request{(total == 1 ? "" : "s")} used";
        }

        HistoryPanel.Visibility = Visibility.Visible;
        PositionNearTray();
    }

    private void CloseHistoryPanel_Click(object sender, RoutedEventArgs e)
    {
        HistoryPanel.Visibility = Visibility.Collapsed;
        PositionNearTray();
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        if (SettingsViewModel is null) return;
        SettingsWindow win = new() { DataContext = SettingsViewModel };
        SettingsViewModel.CloseRequested = () =>
        {
            SettingsViewModel.CloseRequested = null;
            win.Close();
        };
        Hide();
        win.Show();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    public void PositionNearTray()
    {
        UpdateLayout();
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - ActualWidth - 12;
        Top = workArea.Bottom - ActualHeight - 12;
    }
}
