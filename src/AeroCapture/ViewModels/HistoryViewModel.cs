using System.Collections.ObjectModel;
using AeroCapture.Models;
using AeroCapture.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AeroCapture.ViewModels;

public partial class HistoryViewModel : ObservableObject
{
    public ObservableCollection<RecordingItem> Recordings => HistoryManager.Instance.Recordings;
    public ObservableCollection<ScreenshotItem> Screenshots => HistoryManager.Instance.Screenshots;

    [ObservableProperty]
    private int selectedTab = 0; // 0 = Recordings, 1 = Screenshots

    [RelayCommand]
    private void Refresh()
    {
        HistoryManager.Instance.RefreshHistory();
    }

    [RelayCommand]
    private void OpenRecording(RecordingItem item)
    {
        HistoryManager.OpenFile(item.FilePath);
    }

    [RelayCommand]
    private void RevealRecording(RecordingItem item)
    {
        HistoryManager.RevealInExplorer(item.FilePath);
    }

    [RelayCommand]
    private void DeleteRecording(RecordingItem item)
    {
        HistoryManager.Instance.DeleteRecording(item);
    }

    [RelayCommand]
    private void OpenScreenshot(ScreenshotItem item)
    {
        HistoryManager.OpenFile(item.FilePath);
    }

    [RelayCommand]
    private void RevealScreenshot(ScreenshotItem item)
    {
        HistoryManager.RevealInExplorer(item.FilePath);
    }

    [RelayCommand]
    private void DeleteScreenshot(ScreenshotItem item)
    {
        HistoryManager.Instance.DeleteScreenshot(item);
    }
}
