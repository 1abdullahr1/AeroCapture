using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using AeroCapture.Models;
using AeroCapture.ViewModels;

namespace AeroCapture.Views;

public sealed partial class HistoryPage : Page
{
    public HistoryViewModel ViewModel { get; } = new();

    public HistoryPage()
    {
        InitializeComponent();
    }

    private void OnRefreshClicked(object sender, RoutedEventArgs e)
    {
        ViewModel.RefreshCommand.Execute(null);
    }

    private void OnTabSelectionChanged(TabView sender, TabViewSelectionChangedEventArgs args)
    {
        ViewModel.SelectedTab = sender.SelectedIndex;
    }

    private void OnOpenRecordingClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is RecordingItem item)
        {
            ViewModel.OpenRecordingCommand.Execute(item);
        }
    }

    private void OnRevealRecordingClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is RecordingItem item)
        {
            ViewModel.RevealRecordingCommand.Execute(item);
        }
    }

    private void OnDeleteRecordingClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is RecordingItem item)
        {
            ViewModel.DeleteRecordingCommand.Execute(item);
        }
    }

    private void OnOpenScreenshotClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is ScreenshotItem item)
        {
            ViewModel.OpenScreenshotCommand.Execute(item);
        }
    }

    private void OnRevealScreenshotClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is ScreenshotItem item)
        {
            ViewModel.RevealScreenshotCommand.Execute(item);
        }
    }

    private void OnDeleteScreenshotClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is ScreenshotItem item)
        {
            ViewModel.DeleteScreenshotCommand.Execute(item);
        }
    }
}
