using System.Configuration;
using System.Data;
using System.Windows;

namespace MisaImageEditor.Desktop;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private static readonly string ErrorLogPath = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MisaImageEditor",
        "startup-error.log");

    public App()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    private static void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        WriteError("DispatcherUnhandledException", e.Exception);
        System.Windows.MessageBox.Show(
            $"MISA Image Editor gặp lỗi khởi động hoặc thao tác:\n\n{e.Exception.Message}\n\nLog: {ErrorLogPath}",
            "MISA Image Editor",
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Error);
        e.Handled = true;
    }

    private static void OnUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception) WriteError("UnhandledException", exception);
        else WriteError("UnhandledException", new Exception(e.ExceptionObject?.ToString() ?? "Unknown exception"));
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        WriteError("UnobservedTaskException", e.Exception);
        e.SetObserved();
    }

    private static void WriteError(string source, Exception exception)
    {
        try
        {
            var directory = System.IO.Path.GetDirectoryName(ErrorLogPath);
            if (!string.IsNullOrWhiteSpace(directory)) System.IO.Directory.CreateDirectory(directory);
            System.IO.File.AppendAllText(ErrorLogPath, $"[{DateTimeOffset.Now:O}] {source}\n{exception}\n\n");
        }
        catch
        {
            // Logging must never hide the original exception.
        }
    }
}
