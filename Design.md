Goal: Create a program that intercepts audio destined for a specific device and duplicates that audio to a second device.

Components:
    A small UI where you:
        select the source audio device
        Select the destination audio device for the duplicated data
        enable or disable the duplication process
        starts or stops the worker task that performs the audio duplication
        potentially a switch that will attempt to bluetooth reconnect the device if it is disconnected
    A worker task that duplicates the audio
    (optionally) A worker task that will attempt to reconnect the bluetooth destination device if it is missing.

================================================================================
IMPLEMENTATION PLAN - CONTEXT FOR NEW SESSION
================================================================================

## Technology Stack Decisions

**Platform**: Windows only
**Language**: C# (.NET 8.0 or later)
**UI Framework**: WPF (Windows Presentation Foundation)
**Audio Library**: NAudio (for WASAPI audio capture/playback)
**Architecture**: MVVM (Model-View-ViewModel) pattern

**Rationale for C# + WPF**:
- Native Windows Audio APIs (WASAPI) for low-latency audio capture and playback
- Excellent audio libraries available (NAudio)
- Modern UI with XAML
- Strong threading support (async/await, Task Parallel Library)
- Good performance for real-time audio streaming

## System Architecture

```
UI Layer (MainWindow.xaml)
    ↓
ViewModel (AudioDuplicationViewModel)
    ↓
Services Layer:
    - AudioDeviceEnumerator (lists available devices)
    - AudioCaptureService (captures audio from source)
    - AudioPlaybackService (plays audio to destination)
    - AudioDuplicationWorker (background task for duplication)
    - BluetoothReconnectWorker (optional, reconnects BT devices)
    ↓
Audio Layer (WASAPI Engine)
    ↓
Physical Audio Devices
```

## Project Structure

```
AudioDuplication/
├── AudioDuplication.sln
├── AudioDuplication/
│   ├── App.xaml
│   ├── App.xaml.cs
│   ├── MainWindow.xaml
│   ├── MainWindow.xaml.cs
│   ├── ViewModels/
│   │   └── AudioDuplicationViewModel.cs
│   ├── Services/
│   │   ├── IAudioDeviceEnumerator.cs
│   │   ├── AudioDeviceEnumerator.cs
│   │   ├── IAudioCaptureService.cs
│   │   ├── AudioCaptureService.cs
│   │   ├── IAudioPlaybackService.cs
│   │   ├── AudioPlaybackService.cs
│   │   ├── IAudioDuplicationWorker.cs
│   │   ├── AudioDuplicationWorker.cs
│   │   ├── IBluetoothReconnectWorker.cs
│   │   └── BluetoothReconnectWorker.cs
│   ├── Models/
│   │   └── AudioDeviceInfo.cs
│   └── Resources/
│       └── Styles.xaml
└── packages/
```

## UI Layout (MainWindow.xaml)

```
+----------------------------------+
| Audio Duplication Application    |
+----------------------------------+
| Source Device:    [Dropdown v]   |
| Destination Dev:  [Dropdown v]   |
|                                  |
| [x] Enable Duplication           |
|                                  |
| [Start Duplication] [Stop]      |
|                                  |
| Status: Idle / Active / Error    |
|                                  |
| [x] Auto-Reconnect Bluetooth     |
| BT Status: Connected / Disconnected|
+----------------------------------+
```

## Component Details

### 1. Audio Device Enumerator Service
- Enumerate all available audio output devices
- Enumerate all available audio input devices
- Provide device information (name, ID, state)
- Monitor device connection/disconnection events

### 2. Audio Capture Service
- Initialize WASAPI capture from source device
- Capture audio data in real-time
- Provide audio buffer to duplication worker
- Handle format conversion if needed

### 3. Audio Playback Service
- Initialize WASAPI playback to destination device
- Play audio data received from duplication worker
- Handle buffer management and timing
- Handle format conversion if needed

### 4. Audio Duplication Worker
- Run as background task
- Continuously read from capture service
- Write to playback service
- Handle synchronization and timing
- Report status to ViewModel

### 5. Bluetooth Reconnect Worker (Optional)
- Monitor destination device connection status
- Attempt to reconnect if device is disconnected
- Report connection status to ViewModel
- Respect auto-reconnect toggle setting

## Implementation Steps (Todo List)

[x] Read and understand design requirements
[-] Create project structure and solution setup
[ ] Design and implement the main UI window (MainWindow.xaml)
[ ] Implement audio device enumeration service (list available input/output devices)
[ ] Implement audio capture service using WASAPI/NAudio
[ ] Implement audio playback service using WASAPI/NAudio
[ ] Create audio duplication worker task (background thread)
[ ] Implement start/stop controls for the duplication process
[ ] Implement enable/disable toggle for duplication
[ ] Add device selection dropdowns (source and destination)
[ ] Implement status indicators (active/inactive, device connection status)
[ ] (Optional) Implement Bluetooth reconnection worker task
[ ] Add error handling and user notifications
[ ] Test audio capture from source device
[ ] Test audio playback to destination device
[ ] Test full audio duplication workflow
[ ] Create documentation and usage instructions

## Key Technical Considerations

### Audio Format Handling
- Source and destination devices may have different sample rates/bit depths
- Need to handle format conversion using NAudio's resampling capabilities
- Buffer size optimization to minimize latency

### Threading
- Audio capture/playback must run on dedicated threads
- UI updates must be marshaled to UI thread
- Use Task/async-await for background operations

### Error Handling
- Device disconnection during active duplication
- Format incompatibility between devices
- Insufficient system resources
- Permission issues

### Performance
- Minimize latency between capture and playback
- Efficient buffer management
- Avoid blocking UI thread

## Dependencies

### NuGet Packages
- `NAudio` - Core audio library
- `Microsoft.Extensions.DependencyInjection` - DI container (optional)
- `CommunityToolkit.Mvvm` - MVVM helpers (optional)

### System Requirements
- Windows 10 or later
- .NET 8.0 Runtime
- Audio devices (at least one output device)

## Success Criteria

1. User can select source and destination audio devices
2. Audio from source device is duplicated to destination device
3. Start/stop controls work correctly
4. Enable/disable toggle works correctly
5. Status indicators accurately reflect system state
6. Application handles device disconnection gracefully
7. (Optional) Bluetooth auto-reconnect works as expected
8. Low latency audio duplication (< 100ms)

## Data Flow

1. User selects source and destination devices from dropdowns
2. User clicks "Start Duplication"
3. DuplicationWorker starts background task
4. AudioCaptureService begins capturing from source device
5. AudioPlaybackService begins playback to destination device
6. Worker continuously reads from capture and writes to playback
7. User clicks "Stop" to terminate the process
8. Services are stopped and resources released

## Notes for Next Session

- The detailed plan is also saved in `plans/audio-duplication-plan.md`
- When starting implementation, begin with Phase 1: Project Setup
- Use the todo list to track progress
- Update the todo list as tasks are completed
- Switch to Code mode when ready to start implementation


