using System;
using Microsoft.UI.Xaml;

namespace AeroCapture;

public partial class App : Application
{
    private Window? _mainWindow;

    public App()
    {
        InitializeComponent();

        UnhandledException += (s, e) =>
        {
            try
            {
                string logPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                    "AeroCapture_crash.log");

                System.IO.File.AppendAllText(logPath, $"[{DateTime.Now}] Unhandled Exception: {e.Message}\n{e.Exception}\n\n");
                MessageBox(IntPtr.Zero, $"{e.Message}\n\nDetails saved to: {logPath}", "AeroCapture Error", 0x10 /* MB_ICONERROR */);
            }
            catch { }
        };
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            _mainWindow = new MainWindow();
            _mainWindow.Activate();
        }
        catch (Exception ex)
        {
            string logPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                "AeroCapture_crash.log");
            System.IO.File.AppendAllText(logPath, $"[{DateTime.Now}] Launch Exception: {ex.Message}\n{ex}\n\n");
            MessageBox(IntPtr.Zero, $"Launch failed: {ex.Message}\n\nDetails saved to: {logPath}", "AeroCapture Error", 0x10);
        }
    }
}
