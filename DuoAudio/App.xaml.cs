using System.Configuration;
using System.Data;
using System.Windows;

namespace DuoAudio;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        // Add global exception handler for unhandled exceptions
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        DispatcherUnhandledException += OnDispatcherUnhandledException;
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Unhandled exception: {ex.Message}\n{ex.StackTrace}");
            
            // Show user-friendly error message
            System.Windows.MessageBox.Show(
                $"An unexpected error occurred: {ex.Message}\n\nThe application will continue running.",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"Dispatcher unhandled exception: {e.Exception.Message}\n{e.Exception.StackTrace}");
        
        // Show user-friendly error message
        System.Windows.MessageBox.Show(
            $"An unexpected error occurred: {e.Exception.Message}\n\nThe application will continue running.",
            "Error",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        
        // Mark exception as handled to prevent application crash
        e.Handled = true;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("App.OnStartup called");
            base.OnStartup(e);
            System.Diagnostics.Debug.WriteLine("base.OnStartup completed");

            // Get command line arguments
            var args = Environment.GetCommandLineArgs();
            System.Diagnostics.Debug.WriteLine($"Command line args: {string.Join(", ", args)}");

            // Pass command line arguments to MainWindow
            System.Diagnostics.Debug.WriteLine("Creating MainWindow...");
            var mainWindow = new MainWindow(args);
            System.Diagnostics.Debug.WriteLine("MainWindow created successfully");

            mainWindow.Show();
            System.Diagnostics.Debug.WriteLine("MainWindow.Show() completed");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in App.OnStartup: {ex.Message}\n{ex.StackTrace}");
            System.Windows.MessageBox.Show($"Error starting application: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            throw;
        }
    }
}

