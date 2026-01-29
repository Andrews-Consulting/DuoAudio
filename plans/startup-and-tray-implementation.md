# Startup and System Tray Implementation Plan

## Overview
This plan outlines the implementation of startup functionality and system tray integration for the DuoAudio Application.

## Requirements Summary

### User Requirements
1. **Run at Startup**: Checkbox to automatically start the application at Windows startup
2. **Background Execution**: When running at startup, the UI should close while the background task continues
3. **System Tray Icon**: Simple icon to show/hide the UI
4. **Configuration Persistence**: Store source and destination device selection for startup
5. **Single Instance**: Only one instance of the application should run at a time
6. **Menu Bar**: Simple toolbar at the top with an About button
7. **Error Handling**: Show UI with error message if configured devices are unavailable

### Design Decisions
- **Configuration Storage**: Command line arguments (using device IDs internally)
- **System Tray**: Simple icon to show/hide UI (no context menu)
- **Single Instance**: Show existing UI if already running
- **Error Recovery**: Show UI with error message if devices are unavailable
- **Menu Bar**: Simple toolbar at top with About button

## Architecture

### Components

#### 1. System Tray Service
- **File**: `Services/SystemTrayService.cs`
- **Responsibilities**:
  - Create and manage system tray icon
  - Handle tray icon click events (show/hide UI)
  - Dispose of tray icon when application exits

#### 2. Startup Manager
- **File**: `Services/StartupManager.cs`
- **Responsibilities**:
  - Add/remove application from Windows startup folder
  - Create shortcuts with command line arguments
  - Check if application is in startup folder
  - Generate command line arguments with device IDs

#### 3. Command Line Parser
- **File**: `Services/CommandLineParser.cs`
- **Responsibilities**:
  - Parse command line arguments for source/destination device IDs
  - Validate device IDs against available devices
  - Provide parsed configuration to application

#### 4. Single Instance Manager
- **File**: `Services/SingleInstanceManager.cs`
- **Responsibilities**:
  - Detect if another instance is running
  - Bring existing instance to foreground if already running
  - Use named mutex or named pipe for inter-process communication

#### 5. UI Updates
- **Files**: `MainWindow.xaml`, `MainWindow.xaml.cs`
- **Changes**:
  - Add simple toolbar at top with About button
  - Add "Run at Startup" checkbox
  - Implement window close behavior (hide to tray instead of exit)
  - Handle tray icon show/hide events

#### 6. ViewModel Updates
- **File**: `ViewModels/DuoAudioViewModel.cs`
- **Changes**:
  - Add `RunAtStartup` property
  - Add `IsMinimizedToTray` property
  - Handle startup configuration loading

## Implementation Steps

### Step 1: Add System Tray Service
**File**: `Services/SystemTrayService.cs`

```csharp
public interface ISystemTrayService
{
    void Initialize(NotifyIcon icon);
    void Show();
    void Hide();
    void Dispose();
}

public class SystemTrayService : ISystemTrayService, IDisposable
{
    private NotifyIcon? _notifyIcon;
    private Action? _onIconClick;

    public void Initialize(NotifyIcon icon)
    {
        _notifyIcon = icon;
        _notifyIcon.Click += (s, e) => _onIconClick?.Invoke();
    }

    public void SetOnClickAction(Action onClick)
    {
        _onIconClick = onClick;
    }

    public void Show()
    {
        _notifyIcon?.Visible = true;
    }

    public void Hide()
    {
        _notifyIcon?.Visible = false;
    }

    public void Dispose()
    {
        _notifyIcon?.Dispose();
    }
}
```

### Step 2: Add Startup Manager
**File**: `Services/StartupManager.cs`

```csharp
public interface IStartupManager
{
    bool IsInStartup();
    void AddToStartup(string sourceDeviceId, string destinationDeviceId);
    void RemoveFromStartup();
}

public class StartupManager : IStartupManager
{
    private const string StartupFolder = @"%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup";
    private const string ShortcutName = "DuoAudio.lnk";

    public bool IsInStartup()
    {
        var shortcutPath = GetShortcutPath();
        return File.Exists(shortcutPath);
    }

    public void AddToStartup(string sourceDeviceId, string destinationDeviceId)
    {
        var exePath = Process.GetCurrentProcess().MainModule?.FileName;
        var shortcutPath = GetShortcutPath();
        var arguments = $"--source-id \"{sourceDeviceId}\" --dest-id \"{destinationDeviceId}\"";

        // Create shortcut using WScript.Shell
        var shell = new WshShell();
        var shortcut = (IWshShortcut)shell.CreateShortcut(shortcutPath);
        shortcut.TargetPath = exePath;
        shortcut.Arguments = arguments;
        shortcut.Description = "DuoAudio Application";
        shortcut.Save();
    }

    public void RemoveFromStartup()
    {
        var shortcutPath = GetShortcutPath();
        if (File.Exists(shortcutPath))
        {
            File.Delete(shortcutPath);
        }
    }

    private string GetShortcutPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        return Path.Combine(appData, ShortcutName);
    }
}
```

### Step 3: Add Command Line Parser
**File**: `Services/CommandLineParser.cs`

```csharp
public class CommandLineConfig
{
    public string? SourceDeviceId { get; set; }
    public string? DestinationDeviceId { get; set; }
}

public interface ICommandLineParser
{
    CommandLineConfig Parse(string[] args);
}

public class CommandLineParser : ICommandLineParser
{
    public CommandLineConfig Parse(string[] args)
    {
        var config = new CommandLineConfig();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLower())
            {
                case "--source-id":
                    if (i + 1 < args.Length)
                        config.SourceDeviceId = args[++i];
                    break;
                case "--dest-id":
                    if (i + 1 < args.Length)
                        config.DestinationDeviceId = args[++i];
                    break;
            }
        }

        return config;
    }
}
```

### Step 4: Add Single Instance Manager
**File**: `Services/SingleInstanceManager.cs`

```csharp
public interface ISingleInstanceManager
{
    bool IsFirstInstance { get; }
    void Initialize();
    void SignalExistingInstance();
}

public class SingleInstanceManager : ISingleInstanceManager
{
    private const string MutexName = "DuoAudio_SingleInstance_Mutex";
    private Mutex? _mutex;

    public bool IsFirstInstance { get; private set; }

    public void Initialize()
    {
        _mutex = new Mutex(true, MutexName, out bool createdNew);
        IsFirstInstance = createdNew;
    }

    public void SignalExistingInstance()
    {
        // Use named pipe or event to signal existing instance
        // For simplicity, we'll use a named event
        using var eventHandle = new EventWaitHandle(false, EventResetMode.AutoReset, "DuoAudio_ShowWindow");
        eventHandle.Set();
    }
}
```

### Step 5: Update MainWindow XAML
**File**: `MainWindow.xaml`

Changes:
1. Add simple toolbar at top with About button
2. Add "Run at Startup" checkbox
3. Add NotifyIcon component

```xml
<Window x:Class="DuoAudio.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:local="clr-namespace:DuoAudio"
        mc:Ignorable="d"
        Title="DuoAudio Application" Height="500" Width="650"
        Closing="OnWindowClosing">
    <Grid Margin="20">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>  <!-- Toolbar -->
            <RowDefinition Height="Auto"/>  <!-- Source Device -->
            <RowDefinition Height="Auto"/>  <!-- Destination Device -->
            <RowDefinition Height="Auto"/>  <!-- Latency Control -->
            <RowDefinition Height="Auto"/>  <!-- Control Button -->
            <RowDefinition Height="Auto"/>  <!-- Worker Status -->
            <RowDefinition Height="Auto"/>  <!-- Status -->
            <RowDefinition Height="Auto"/>  <!-- Bluetooth Section -->
            <RowDefinition Height="Auto"/>  <!-- Run at Startup -->
            <RowDefinition Height="*"/>      <!-- Spacer -->
        </Grid.RowDefinitions>

        <!-- Toolbar -->
        <ToolBar Grid.Row="0" Margin="0,0,0,15">
            <Button x:Name="AboutButton" Content="About" Padding="10,5" Click="OnAboutClick"/>
        </ToolBar>

        <!-- Source Device -->
        <TextBlock Grid.Row="1" Text="Source Device (Audio Output):" FontWeight="Bold" Margin="0,0,0,5" FontSize="14"/>
        <ComboBox Grid.Row="2" x:Name="SourceDeviceComboBox" Margin="0,0,0,15" Padding="5" FontSize="12"/>

        <!-- Destination Device -->
        <TextBlock Grid.Row="3" Text="Destination Device:" FontWeight="Bold" Margin="0,0,0,5" FontSize="14"/>
        <ComboBox Grid.Row="4" x:Name="DestinationDeviceComboBox" Margin="0,0,0,15" Padding="5" FontSize="12"/>

        <!-- Latency Control -->
        <Grid Grid.Row="5" Margin="0,0,0,15">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>
            <TextBlock Grid.Column="0" Text="Buffer Size (Latency):" FontWeight="Bold" VerticalAlignment="Center" Margin="0,0,10,0"/>
            <Slider Grid.Column="1" x:Name="BufferSizeSlider" Minimum="50" Maximum="500" Value="100" TickFrequency="50" TickPlacement="BottomRight"/>
            <TextBlock Grid.Column="2" x:Name="BufferSizeValue" Text="100ms" Margin="10,0,0,0" VerticalAlignment="Center" Width="50"/>
        </Grid>

        <!-- Control Button -->
        <Button Grid.Row="6" x:Name="ToggleWorkerButton" Content="Start Worker Task" Width="150" Padding="10,8" Margin="0,0,0,15" FontSize="12" FontWeight="Bold" HorizontalAlignment="Left" Click="OnToggleWorkerButtonClick"/>

        <!-- Worker Status Indicator -->
        <Border Grid.Row="7" x:Name="WorkerStatusBorder" Background="LightGray" Padding="10,5" Margin="0,0,0,15" CornerRadius="3" HorizontalAlignment="Left">
            <TextBlock x:Name="WorkerStatusTextBlock" Text="Worker Status: Stopped" FontWeight="Bold" FontSize="12"/>
        </Border>

        <!-- Status -->
        <TextBlock Grid.Row="8" x:Name="StatusTextBlock" Text="Status: Idle" FontWeight="Bold" Margin="0,0,0,15" FontSize="12" Foreground="DarkBlue"/>

        <!-- Bluetooth Section -->
        <Grid Grid.Row="9" Margin="0,0,0,5">
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
            </Grid.RowDefinitions>

            <!-- Bluetooth Auto-Reconnect -->
            <CheckBox Grid.Row="0" x:Name="AutoReconnectCheckBox" Content="Auto-Reconnect Bluetooth" Margin="0,0,0,5" FontSize="12"/>

            <!-- Bluetooth Status -->
            <TextBlock Grid.Row="1" x:Name="BluetoothStatusTextBlock" Text="BT Status: Unknown" FontWeight="Normal" FontSize="11" Margin="20,0,0,0" Foreground="Gray"/>
        </Grid>

        <!-- Run at Startup -->
        <CheckBox Grid.Row="10" x:Name="RunAtStartupCheckBox" Content="Run at Startup" Margin="0,0,0,15" FontSize="12" IsEnabled="False"/>
    </Grid>

    <!-- System Tray Icon -->
    <Window.Resources>
        <ResourceDictionary>
            <BitmapImage x:Key="TrayIcon" UriSource="/Resources/trayicon.ico"/>
        </ResourceDictionary>
    </Window.Resources>
</Window>
```

### Step 6: Update MainWindow Code-Behind
**File**: `MainWindow.xaml.cs`

Key changes:
1. Initialize system tray service
2. Handle window closing (hide to tray instead of exit)
3. Handle tray icon click (show window)
4. Handle "Run at Startup" checkbox
5. Load command line configuration
6. Check if in startup and update checkbox
7. Enable/disable checkbox based on device selection and worker status

```csharp
public partial class MainWindow : Window
{
    private DuoAudioViewModel? _viewModel;
    private ISystemTrayService? _systemTrayService;
    private IStartupManager? _startupManager;
    private ICommandLineParser? _commandLineParser;
    private ISingleInstanceManager? _singleInstanceManager;
    private NotifyIcon? _notifyIcon;

    public MainWindow(string[] args)
    {
        InitializeComponent();
        InitializeServices();
        InitializeViewModel();
        ProcessCommandLine(args);
        InitializeSystemTray();
        CheckStartupStatus();
    }

    private void InitializeServices()
    {
        _systemTrayService = new SystemTrayService();
        _startupManager = new StartupManager();
        _commandLineParser = new CommandLineParser();
        _singleInstanceManager = new SingleInstanceManager();
        _singleInstanceManager.Initialize();

        if (!_singleInstanceManager.IsFirstInstance)
        {
            _singleInstanceManager.SignalExistingInstance();
            Application.Current.Shutdown();
            return;
        }
    }

    private void InitializeSystemTray()
    {
        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Visible = false,
            Text = "DuoAudio"
        };

        _systemTrayService?.Initialize(_notifyIcon);
        _systemTrayService?.SetOnClickAction(() =>
        {
            ShowWindow();
        });
    }

    private void ShowWindow()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void HideToTray()
    {
        Hide();
        _systemTrayService?.Show();
    }

    private void OnWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        // If worker is running, hide to tray instead of closing
        if (_viewModel?.IsRunning == true)
        {
            e.Cancel = true;
            HideToTray();
        }
        else
        {
            _systemTrayService?.Dispose();
        }
    }

    private void ProcessCommandLine(string[] args)
    {
        var config = _commandLineParser?.Parse(args);

        if (config?.SourceDeviceId != null && config?.DestinationDeviceId != null)
        {
            // Load devices and select them
            LoadDevicesAndSelect(config.SourceDeviceId, config.DestinationDeviceId);
        }
    }

    private void LoadDevicesAndSelect(string sourceId, string destinationId)
    {
        // Implementation to load devices and select by ID
        // This will be called during startup
    }

    private void CheckStartupStatus()
    {
        var isInStartup = _startupManager?.IsInStartup() ?? false;
        RunAtStartupCheckBox.IsChecked = isInStartup;
    }

    private void OnRunAtStartupChecked(object? sender, RoutedEventArgs e)
    {
        if (_viewModel?.SelectedSourceDevice == null || _viewModel?.SelectedDestinationDevice == null)
        {
            RunAtStartupCheckBox.IsChecked = false;
            return;
        }

        if (RunAtStartupCheckBox.IsChecked == true)
        {
            _startupManager?.AddToStartup(
                _viewModel.SelectedSourceDevice.Id,
                _viewModel.SelectedDestinationDevice.Id
            );
        }
        else
        {
            _startupManager?.RemoveFromStartup();
        }
    }

    private void UpdateRunAtStartupCheckbox()
    {
        RunAtStartupCheckBox.IsEnabled =
            _viewModel?.SelectedSourceDevice != null &&
            _viewModel?.SelectedDestinationDevice != null &&
            _viewModel?.IsRunning == true;
    }
}
```

### Step 7: Update ViewModel
**File**: `ViewModels/DuoAudioViewModel.cs`

Add properties for startup configuration:

```csharp
public class DuoAudioViewModel : INotifyPropertyChanged
{
    // ... existing properties ...

    private bool _runAtStartup;
    public bool RunAtStartup
    {
        get => _runAtStartup;
        set
        {
            _runAtStartup = value;
            OnPropertyChanged();
        }
    }

    private bool _isMinimizedToTray;
    public bool IsMinimizedToTray
    {
        get => _isMinimizedToTray;
        set
        {
            _isMinimizedToTray = value;
            OnPropertyChanged();
        }
    }
}
```

### Step 8: Add Dependencies
**File**: `DuoAudio.csproj`

Add required NuGet packages:

```xml
<ItemGroup>
  <COMReference Include="IWshRuntimeLibrary">
    <Guid>{F935DC20-1CF0-11D0-ADB9-00C04FD58A0B}</Guid>
    <VersionMajor>1</VersionMajor>
    <VersionMinor>0</VersionMinor>
    <Lcid>0</Lcid>
    <WrapperTool>tlbimp</WrapperTool>
    <Isolated>false</Isolated>
    <EmbedInteropTypes>true</EmbedInteropTypes>
  </COMReference>
</ItemGroup>
```

## Testing Plan

### Unit Tests
1. Test `StartupManager` - Add/Remove from startup
2. Test `CommandLineParser` - Parse various argument combinations
3. Test `SingleInstanceManager` - Detect multiple instances

### Integration Tests
1. Test startup with valid devices
2. Test startup with invalid devices (should show error)
3. Test system tray show/hide
4. Test single instance behavior
5. Test "Run at Startup" checkbox enable/disable logic

### Manual Tests
1. Add to startup and restart computer
2. Remove from startup and verify
3. Close UI while running (should hide to tray)
4. Click tray icon (should show UI)
5. Try to open second instance (should show existing UI)

## Error Handling

### Device Not Found
- Show UI with error message
- Highlight missing devices in dropdowns
- Allow user to select different devices

### Startup Folder Access Denied
- Show error message to user
- Log error to debug output
- Don't crash application

### Single Instance Detection Failure
- Log error to debug output
- Allow application to continue (may have multiple instances)

## Future Enhancements

1. **Auto-Recovery**: Automatically find similar devices if configured devices are unavailable
2. **Device Name Enhancement**: Improve dropdown display to show device IDs or additional info
3. **Configuration File**: Option to use JSON config file instead of command line
4. **Tray Context Menu**: Add context menu with options (Show, Stop, Exit)
5. **Device Monitoring**: Automatically restart if devices become available
6. **Logging**: Add logging for troubleshooting startup issues

## Notes

- Device IDs are used internally for uniqueness, but user sees device names in UI
- Command line arguments are hidden from user (created by startup shortcut)
- System tray icon should be simple - just show/hide UI
- Application should gracefully handle missing devices at startup
- Single instance detection uses named mutex for simplicity
