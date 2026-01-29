# Audio Duplication Application

A Windows WPF application that intercepts audio from a source device and duplicates it to a destination device in real-time.

## Features

- **Real-time Audio Duplication**: Capture audio from any input or output device and play it to another device
- **Device Enumeration**: Automatically lists all available audio input and output devices
- **WASAPI Support**: Low-latency audio capture and playback using Windows Audio Session API
- **Loopback Capture**: Can capture system audio (what you hear) from output devices
- **Bluetooth Auto-Reconnect**: Optional feature to monitor and reconnect Bluetooth devices
- **MVVM Architecture**: Clean separation of concerns with Model-View-ViewModel pattern

## Requirements

- Windows 10 or later
- .NET 8.0 or later (built with .NET 9.0)
- At least two audio devices (source and destination)

## Installation

### Building from Source

1. Clone or download the repository
2. Open a terminal in the `AudioDuplication` folder
3. Build the project:
   ```bash
   dotnet build
   ```
4. Run the application:
   ```bash
   dotnet run
   ```

### Running the Executable

After building, the executable will be located at:
```
bin/Debug/net9.0-windows/AudioDuplication.exe
```

## Usage

### Basic Operation

1. **Select Source Device**: Choose the audio output device to capture from
   - Select an output device (speakers, headphones)
   - The app captures system audio being sent to this device using loopback capture

2. **Select Destination Device**: Choose the device to play audio to
   - Must be an output device (speakers, headphones, etc.)

3. **Enable Duplication**: Check the "Enable Duplication" checkbox

4. **Start Duplication**: Click the "Start Duplication" button

5. **Stop Duplication**: Click the "Stop" button when finished

### Bluetooth Auto-Reconnect (Optional)

1. Check the "Auto-Reconnect Bluetooth" checkbox
2. The application will monitor the destination device connection
3. If the device disconnects, the application will attempt to reconnect automatically
4. The "BT Status" indicator shows the current connection state

### Status Indicators

- **Status**: Shows current application state
  - "Idle" - Not duplicating audio
  - "Active" - Currently duplicating audio
  - "Error: ..." - Error message if something goes wrong

- **BT Status**: Shows Bluetooth device connection state (when auto-reconnect is enabled)
  - "Connected" - Device is connected
  - "Disconnected" - Device is disconnected

## Architecture

### Project Structure

```
AudioDuplication/
├── Models/
│   └── AudioDeviceInfo.cs          # Audio device data model
├── Services/
│   ├── IAudioDeviceEnumerator.cs   # Device enumeration interface
│   ├── AudioDeviceEnumerator.cs    # NAudio-based device enumeration
│   ├── IAudioCaptureService.cs     # Audio capture interface
│   ├── AudioCaptureService.cs      # WASAPI audio capture
│   ├── IAudioPlaybackService.cs    # Audio playback interface
│   ├── AudioPlaybackService.cs     # WASAPI audio playback
│   ├── IAudioDuplicationWorker.cs  # Duplication worker interface
│   ├── AudioDuplicationWorker.cs   # Core duplication logic
│   ├── IBluetoothReconnectWorker.cs # Bluetooth reconnect interface
│   └── BluetoothReconnectWorker.cs # Bluetooth monitoring
├── ViewModels/
│   └── AudioDuplicationViewModel.cs # Main ViewModel
├── MainWindow.xaml                 # Main UI
└── MainWindow.xaml.cs              # UI code-behind
```

### Technology Stack

- **Language**: C# (.NET 9.0)
- **UI Framework**: WPF (Windows Presentation Foundation)
- **Audio Library**: NAudio 2.2.1
- **Architecture**: MVVM (Model-View-ViewModel)
- **Audio API**: WASAPI (Windows Audio Session API)

### Data Flow

1. User selects source and destination devices
2. `AudioDuplicationWorker` initializes capture and playback services
3. `AudioCaptureService` captures audio from source device
4. Captured audio triggers `DataAvailable` event
5. `AudioDuplicationWorker` receives audio data and queues it to playback
6. `AudioPlaybackService` plays audio to destination device

## Troubleshooting

### Common Issues

**"Error: Duplication not enabled"**
- Check the "Enable Duplication" checkbox before starting

**"Error: Please select both source and destination devices"**
- Select devices from both dropdowns before starting

**"Error: Duplication worker not initialized"**
- Select both source and destination devices to initialize the worker

**No audio is being duplicated**
- Ensure the source device is producing audio
- Check that the destination device volume is not muted
- Verify devices are not being used exclusively by another application

**Bluetooth device not reconnecting**
- Ensure "Auto-Reconnect Bluetooth" is checked
- Some Bluetooth devices may require manual reconnection in Windows settings
- The device name matching may not work if the device name has changed

### Debug Output

The application writes debug information to the Visual Studio Output window or DebugView. Look for messages prefixed with the service name for troubleshooting.

## Technical Details

### Audio Format

- Sample Rate: 44.1 kHz (CD quality)
- Bit Depth: 16-bit
- Channels: Stereo (2 channels)
- Buffer Duration: 1 second (playback)

### Performance

- Latency: < 100ms typical
- CPU Usage: Minimal (event-driven architecture)
- Memory Usage: Low (circular buffers with size limits)

### Threading

- UI Thread: Handles user interface updates
- Capture Thread: NAudio's internal WASAPI capture thread
- Playback Thread: NAudio's internal WASAPI playback thread
- Duplication Thread: Background task for coordination
- Bluetooth Monitor Thread: Timer-based monitoring (5-second intervals)

## License

This project is provided as-is for educational and personal use.

## Acknowledgments

- [NAudio](https://github.com/naudio/NAudio) - .NET audio library
- Windows Audio Session API (WASAPI) - Low-level audio API
