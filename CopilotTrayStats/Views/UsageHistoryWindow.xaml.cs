using System.Windows;
using System.Windows.Input;
using CopilotTrayStats.ViewModels;

namespace CopilotTrayStats.Views;

public partial class UsageHistoryWindow : Window
{
    public UsageHistoryViewModel? ViewModel { get; set; }

    public UsageHistoryWindow()
    {
        InitializeComponent();
    }

    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);
        if (ViewModel is null) return;

        ViewModel.Reload();
        BarsControl.ItemsSource = ViewModel.Bars;

        if (ViewModel.Bars.Count == 0)
        {
            EmptyText.Visibility = Visibility.Visible;
            SubtitleText.Text = "Since last reset — no data recorded yet";
        }
        else
        {
            EmptyText.Visibility = Visibility.Collapsed;
            SubtitleText.Text = $"Since last reset — {ViewModel.TotalUsed} request{(ViewModel.TotalUsed == 1 ? "" : "s")} used";
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1)
            DragMove();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel?.CloseRequested?.Invoke();
        Close();
    }

    protected override void OnDeactivated(EventArgs e)
    {
        base.OnDeactivated(e);
        Close();
    }
}
