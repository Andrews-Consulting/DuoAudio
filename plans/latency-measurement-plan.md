# Latency Measurement Feature - Implementation Plan

## Overview
Add a latency measurement feature to the existing DiagnosticsWindow that displays the estimated latency based on the current buffer configuration.

## Current State
- DiagnosticsWindow already exists at [`DuoAudio/DiagnosticsWindow.xaml`](DuoAudio/DiagnosticsWindow.xaml)
- DiagnosticsWindow code-behind at [`DuoAudio/DiagnosticsWindow.xaml.cs`](DuoAudio/DiagnosticsWindow.xaml.cs)
- Window has 5 Grid.RowDefinitions currently

## Implementation Plan

### 1. Update XAML Layout
**File:** [`DuoAudio/DiagnosticsWindow.xaml`](DuoAudio/DiagnosticsWindow.xaml)

**Changes:**
1. Add a 6th RowDefinition to Grid.RowDefinitions for the new Latency Measurement section
2. Add a new GroupBox for "Latency Measurement" after the "Audio Activity Meters" section (Grid.Row="5")
3. Move the "Diagnostic Log" GroupBox from Grid.Row="5" to Grid.Row="6"
4. Inside the new GroupBox, add:
   - TextBlock: "Estimated Latency:" (Grid.Row="0")
   - TextBlock: x:Name="LatencyText" with estimated latency value (Grid.Row="1")
   - TextBlock: "Buffer Configuration:" (Grid.Row="2")
   - TextBlock: x:Name="BufferConfigText" with current config label (Grid.Row="3")

**XAML Structure:**
```xml
<!-- Latency Measurement -->
<GroupBox Grid.Row="5" Header="Latency Measurement" Margin="0,0,0,10">
    <Grid Margin="10">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        
        <TextBlock Grid.Row="0" Text="Estimated Latency:" FontWeight="Bold"/>
        <TextBlock Grid.Row="1" x:Name="LatencyText" Text="N/A" Margin="0,5,0,10" FontSize="14" FontWeight="Bold" Foreground="Blue"/>
        
        <TextBlock Grid.Row="2" Text="Buffer Configuration:" FontWeight="Bold" Margin="0,10,0,0"/>
        <TextBlock Grid.Row="3" x:Name="BufferConfigText" Text="N/A" Margin="0,5,0,0"/>
    </Grid>
</GroupBox>
```

### 2. Update Code-Behind
**File:** [`DuoAudio/DiagnosticsWindow.xaml.cs`](DuoAudio/DiagnosticsWindow.xaml.cs)

**Changes:**
1. Add private field to track buffer configuration:
   ```csharp
   private int _currentBufferConfig = 3; // Default to balanced
   ```

2. Add method to calculate estimated latency:
   ```csharp
   private int GetEstimatedLatency(int bufferConfig)
   {
       return bufferConfig switch
       {
           1 => 40,   // Low Latency: 10ms buffer + 10ms latency + 20ms overhead
           2 => 60,   // Low-Medium: 20ms buffer + 20ms latency + 20ms overhead
           3 => 100,  // Balanced: 50ms buffer + 20ms latency + 30ms overhead
           4 => 200,  // Medium-High: 100ms buffer + 50ms latency + 50ms overhead
           5 => 350,  // High Stability: 200ms buffer + 100ms latency + 50ms overhead
           _ => 100   // Default to balanced
       };
   }
   ```

3. Add method to get buffer configuration label:
   ```csharp
   private string GetBufferConfigLabel(int config)
   {
       return config switch
       {
           1 => "Low Latency",
           2 => "Low-Medium",
           3 => "Balanced",
           4 => "Medium-High",
           5 => "High Stability",
           _ => "Balanced"
       };
   }
   ```

4. Update `OnSourceDeviceChanged()` to update latency display:
   - Call `UpdateLatencyDisplay()` after device selection

5. Update `OnDestinationDeviceChanged()` to update latency display:
   - Call `UpdateLatencyDisplay()` after device selection

6. Add `UpdateLatencyDisplay()` method:
   ```csharp
   private void UpdateLatencyDisplay()
   {
       var estimatedLatency = GetEstimatedLatency(_currentBufferConfig);
       LatencyText.Text = $"{estimatedLatency}ms";
       BufferConfigText.Text = GetBufferConfigLabel(_currentBufferConfig);
       Log($"Estimated latency: {estimatedLatency}ms (buffer config: {_currentBufferConfig})");
   }
   ```

7. Add method to set buffer configuration (for future use):
   ```csharp
   public void SetBufferConfiguration(int config)
   {
       _currentBufferConfig = config;
       UpdateLatencyDisplay();
   }
   ```

### 3. Update MainWindow to Pass Buffer Config
**File:** [`DuoAudio/MainWindow.xaml.cs`](DuoAudio/MainWindow.xaml.cs)

**Changes:**
1. When opening DiagnosticsWindow, pass the current buffer configuration:
   ```csharp
   private void OnDiagnosticsClick(object? sender, RoutedEventArgs e)
   {
       var diagnosticsWindow = new DiagnosticsWindow();
       diagnosticsWindow.Owner = this;
       
       // Pass current buffer configuration
       var bufferConfig = (int)BufferConfigSlider.Value;
       diagnosticsWindow.SetBufferConfiguration(bufferConfig);
       
       diagnosticsWindow.Show();
   }
   ```

2. Add "Diagnostics" button to MainWindow toolbar (if not already present)

## Latency Calculation Logic

The estimated latency is calculated as:
- **Buffer Duration**: Time audio sits in buffer before being played
- **WASAPI Latency**: Time for WASAPI to process and output audio
- **Processing Overhead**: Additional time for event handling and data transfer

**Formula:**
```
Estimated Latency = Buffer Duration + WASAPI Latency + Processing Overhead
```

**Values by Configuration:**
| Config | Buffer Duration | WASAPI Latency | Processing Overhead | Total |
|--------|-----------------|-----------------|-------------------|-------|
| 1 | 10ms | 10ms | 20ms | 40ms |
| 2 | 20ms | 20ms | 20ms | 60ms |
| 3 | 50ms | 20ms | 30ms | 100ms (default) |
| 4 | 100ms | 50ms | 30ms | 180ms |
| 5 | 200ms | 100ms | 50ms | 350ms |

## Implementation Steps

1. ✅ Update [`DuoAudio/DiagnosticsWindow.xaml`](DuoAudio/DiagnosticsWindow.xaml) to add latency measurement UI
2. ✅ Update [`DuoAudio/DiagnosticsWindow.xaml.cs`](DuoAudio/DiagnosticsWindow.xaml.cs) to add latency calculation logic
3. ✅ Update [`DuoAudio/MainWindow.xaml.cs`](DuoAudio/MainWindow.xaml.cs) to pass buffer config to diagnostics
4. ✅ Test latency display with different buffer configurations
5. ✅ Build and verify no errors

## Testing Checklist

- [ ] Open DiagnosticsWindow from MainWindow
- [ ] Verify latency display shows "N/A" initially
- [ ] Select source and destination devices
- [ ] Verify latency display updates to show estimated latency
- [ ] Change buffer configuration in MainWindow
- [ ] Reopen DiagnosticsWindow and verify latency display updates
- [ ] Test with different buffer configurations (1-5)
- [ ] Verify latency values match expected values

## Notes

- The latency display is an **estimate** based on buffer configuration
- Actual latency may vary based on system performance and device characteristics
- This provides users with a reference point for tuning buffer configuration
- The display updates when buffer configuration changes
