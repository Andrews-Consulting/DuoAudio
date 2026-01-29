# Audio Duplication Application - User Guide

## Table of Contents
1. [Getting Started](#getting-started)
2. [Basic Usage](#basic-usage)
3. [Advanced Features](#advanced-features)
4. [Use Cases](#use-cases)
5. [Tips and Tricks](#tips-and-tricks)
6. [FAQ](#faq)

## Getting Started

### What is Audio Duplication?

The Audio Duplication Application allows you to route audio from one device to another. For example:
- Play music from your computer to Bluetooth headphones while also hearing it on your speakers
- Route microphone input to a different output device
- Share audio from one application to multiple output devices

### System Requirements

- Windows 10 or Windows 11
- At least 2 audio devices (e.g., speakers + headphones)
- .NET 9.0 Runtime (included with the application)

### First Launch

1. Run `AudioDuplication.exe`
2. The application window will appear with device selection dropdowns
3. Your available audio devices will be automatically detected

## Basic Usage

### Step 1: Select Source Device

The **Source Device** is the audio output device to capture from:

- **Output Devices Only** (speakers, headphones): The app captures system audio being sent to the selected output device
- Uses **WASAPI Loopback Capture** to capture the audio stream

**To capture system audio** (music, videos, game sounds):
- Select your speakers or main output device as the source
- The app captures the audio that would normally play through this device
- Example: Select "Speakers" to capture all system audio going to your speakers

### Step 2: Select Destination Device

The **Destination Device** is where the captured audio will be played:

- Must be an output device (speakers, headphones, Bluetooth devices)
- Can be different from the source device
- Examples: Bluetooth headphones, USB speakers, HDMI audio output

### Step 3: Enable Duplication

Before starting, you must check the **"Enable Duplication"** checkbox. This is a safety feature to prevent accidental audio routing.

### Step 4: Start Duplication

Click the **"Start Duplication"** button. The status will change to "Active" and audio will begin flowing from source to destination.

### Step 5: Stop Duplication

Click the **"Stop"** button to stop the audio duplication. The status will return to "Idle".

## Advanced Features

### Bluetooth Auto-Reconnect

If you're using a Bluetooth device as your destination, you can enable automatic reconnection:

1. Check the **"Auto-Reconnect Bluetooth"** checkbox
2. The application will monitor the Bluetooth device connection
3. If the device disconnects (goes out of range, turns off), the app will attempt to reconnect automatically
4. The **"BT Status"** indicator shows:
   - **Connected**: Device is connected and ready
   - **Disconnected**: Device is not connected

**Note**: This feature works best with Bluetooth audio devices. Some devices may require manual reconnection in Windows Bluetooth settings.

### Device Hot-Swapping

You can change devices while the application is running:

1. Stop the duplication if it's active
2. Select a new source or destination device from the dropdowns
3. The application will automatically reconfigure for the new devices
4. Start duplication again

### Multiple Device Support

The application detects all audio devices connected to your system:
- Built-in speakers
- Headphones (wired and wireless)
- USB audio devices
- HDMI audio outputs
- Bluetooth audio devices
- Virtual audio cables

## Use Cases

### Use Case 1: Share Audio with a Friend

**Scenario**: You want to watch a movie with someone, but only have one pair of Bluetooth headphones.

**Solution**:
1. Source: Your computer speakers (loopback)
2. Destination: Bluetooth headphones
3. Start duplication
4. You hear audio from speakers, your friend hears it on Bluetooth headphones

### Use Case 2: Route Audio to Multiple Rooms

**Scenario**: You have speakers in different rooms and want to play the same music everywhere.

**Solution**:
1. Source: Your main speakers (loopback)
2. Destination: Secondary speakers (Bluetooth or wired)
3. Enable duplication
4. Audio plays from both speaker sets simultaneously

### Use Case 3: Monitor Microphone Output

**Scenario**: You want to hear your own microphone through headphones (monitoring).

**Solution**:
1. Source: Your microphone
2. Destination: Your headphones
3. Enable duplication
4. You hear yourself speaking with minimal latency

### Use Case 4: Record and Play Simultaneously

**Scenario**: You need to record audio while also monitoring it live.

**Solution**:
1. Source: Your microphone or system audio
2. Destination: Your headphones
3. Enable duplication
4. Use separate recording software to capture the source

## Tips and Tricks

### Minimizing Latency

- Use wired connections instead of Bluetooth when possible
- Close unnecessary applications to reduce system load
- Use high-quality audio devices with good drivers

### Avoiding Feedback Loops

**Warning**: Never set the destination device to be the same as the source device when capturing from speakers. This creates a feedback loop (loud screeching noise).

**Safe Configurations**:
- Source: Microphone → Destination: Speakers (safe)
- Source: Speakers → Destination: Headphones (safe)
- Source: Speakers → Destination: Same Speakers (feedback loop!)

### Managing Volume Levels

- Adjust source device volume to control input level
- Adjust destination device volume to control output level
- The application does not modify audio volume - it passes through as-is

### Bluetooth Device Tips

- Keep Bluetooth devices within range (typically 10 meters / 30 feet)
- Ensure Bluetooth devices are fully charged
- Some Bluetooth devices have latency (delay) - this is normal
- If auto-reconnect doesn't work, try manually connecting in Windows settings first

## FAQ

### Q: Why can't I see my device in the dropdown?

**A**: Possible reasons:
- Device is not connected or powered on
- Device is disabled in Windows Sound settings
- Device is being used exclusively by another application
- Try refreshing by unplugging and reconnecting the device

### Q: Why is there a delay in the audio?

**A**: Some latency is normal due to:
- Bluetooth audio has inherent latency (100-300ms)
- Audio buffering for smooth playback
- System processing time

To minimize latency:
- Use wired connections
- Close other applications
- Use high-quality audio devices

### Q: Can I duplicate to multiple destination devices?

**A**: The current version supports one destination device at a time. To route to multiple devices, you would need to run multiple instances of the application (not officially supported).

### Q: Why does the application say "Error: Duplication not enabled"?

**A**: You must check the "Enable Duplication" checkbox before clicking Start. This is a safety feature.

### Q: Can I use this for recording audio?

**A**: This application is designed for real-time duplication only. For recording, use dedicated recording software like Audacity, OBS Studio, or similar tools.

### Q: Does this work with USB audio interfaces?

**A**: Yes! USB audio interfaces, mixers, and professional audio equipment that appear as Windows audio devices will work.

### Q: Why is the audio quality poor?

**A**: The application uses CD-quality audio (44.1kHz, 16-bit, stereo). If quality is poor:
- Check your audio device quality settings
- Ensure you're using high-quality audio cables
- Update your audio device drivers

### Q: Can I minimize the application while it's running?

**A**: Yes! The application continues to duplicate audio when minimized. You can also close the window to stop the application.

### Q: Does this work with applications like Zoom, Teams, or Discord?

**A**: Yes, but with caveats:
- These applications may have their own audio routing
- You may need to configure them to use the correct input/output devices
- Some applications may not work well with loopback capture

### Q: How do I completely uninstall?

**A**: The application is portable - simply delete the folder. No registry entries or system files are modified.

## Getting Help

If you encounter issues:

1. Check the Status message for error details
2. Verify your devices are working in Windows Sound settings
3. Try restarting the application
4. Check the README.md for technical troubleshooting
5. Ensure your audio drivers are up to date

## Keyboard Shortcuts

Currently, the application does not have keyboard shortcuts. All controls must be accessed through the user interface.

## Updates and Version History

### Version 1.0
- Initial release
- Basic audio duplication functionality
- Device enumeration
- Bluetooth auto-reconnect
- WASAPI audio support
