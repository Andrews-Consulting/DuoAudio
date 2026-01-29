using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using DuoAudio.Models;
using DuoAudio.Services;

namespace DuoAudio
{
    public partial class DiagnosticsWindow : Window
    {
        private readonly AudioDeviceEnumerator _deviceEnumerator;
        private WasapiLoopbackCapture? _capture;
        private WasapiOut? _playback;
        private BufferedWaveProvider? _buffer;
        private DispatcherTimer? _meterTimer;
        private float _lastCaptureLevel;
        private float _lastPlaybackLevel;
        private int _currentBufferConfig = 3; // Default to balanced

        public DiagnosticsWindow()
        {
            InitializeComponent();
            _deviceEnumerator = new AudioDeviceEnumerator();
            LoadDevices();
            CheckWindowsDefaultDevice();
            UpdateLatencyDisplay();
            Log("Diagnostics window initialized");
        }

        public void SetBufferConfiguration(int config)
        {
            _currentBufferConfig = config;
            UpdateLatencyDisplay();
        }

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

        private void UpdateLatencyDisplay()
        {
            var estimatedLatency = GetEstimatedLatency(_currentBufferConfig);
            LatencyText.Text = $"{estimatedLatency}ms";
            BufferConfigText.Text = GetBufferConfigLabel(_currentBufferConfig);
            Log($"Estimated latency: {estimatedLatency}ms (buffer config: {_currentBufferConfig})");
        }

        private void LoadDevices()
        {
            var devices = _deviceEnumerator.GetOutputDevices();
            SourceDeviceComboBox.ItemsSource = devices;
            DestinationDeviceComboBox.ItemsSource = devices;
        }

        private void CheckWindowsDefaultDevice()
        {
            try
            {
                var enumerator = new MMDeviceEnumerator();
                var defaultDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                WindowsDefaultText.Text = defaultDevice.FriendlyName;
                Log($"Windows default output device: {defaultDevice.FriendlyName}");
                Log($"Default device ID: {defaultDevice.ID}");
            }
            catch (Exception ex)
            {
                WindowsDefaultText.Text = "Error: " + ex.Message;
                Log($"Error checking default device: {ex.Message}");
            }
        }

        private void OnSourceDeviceChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SourceDeviceComboBox.SelectedItem is AudioDeviceInfo device)
            {
                try
                {
                    var enumerator = new MMDeviceEnumerator();
                    var mmDevice = enumerator.GetDevice(device.Id);
                    var format = mmDevice.AudioClient.MixFormat;
                    SourceFormatText.Text = $"{format.SampleRate}Hz, {format.BitsPerSample}-bit, {format.Channels} channels";
                    Log($"Source device selected: {device.Name}");
                    Log($"  Format: {format.SampleRate}Hz, {format.BitsPerSample}-bit, {format.Channels}ch");
                    Log($"  Device ID: {device.Id}");
                    CheckFormatsMatch();
                }
                catch (Exception ex)
                {
                    SourceFormatText.Text = "Error: " + ex.Message;
                    Log($"Error getting source format: {ex.Message}");
                }
            }
        }

        private void OnDestinationDeviceChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DestinationDeviceComboBox.SelectedItem is AudioDeviceInfo device)
            {
                try
                {
                    var enumerator = new MMDeviceEnumerator();
                    var mmDevice = enumerator.GetDevice(device.Id);
                    var format = mmDevice.AudioClient.MixFormat;
                    DestinationFormatText.Text = $"{format.SampleRate}Hz, {format.BitsPerSample}-bit, {format.Channels} channels";
                    Log($"Destination device selected: {device.Name}");
                    Log($"  Format: {format.SampleRate}Hz, {format.BitsPerSample}-bit, {format.Channels}ch");
                    Log($"  Device ID: {device.Id}");
                    CheckFormatsMatch();
                }
                catch (Exception ex)
                {
                    DestinationFormatText.Text = "Error: " + ex.Message;
                    Log($"Error getting destination format: {ex.Message}");
                }
            }
        }

        private void CheckFormatsMatch()
        {
            if (SourceDeviceComboBox.SelectedItem == null || DestinationDeviceComboBox.SelectedItem == null)
            {
                FormatsMatchText.Text = "Select both devices";
                return;
            }

            try
            {
                var enumerator = new MMDeviceEnumerator();
                var sourceDevice = enumerator.GetDevice(((AudioDeviceInfo)SourceDeviceComboBox.SelectedItem).Id);
                var destDevice = enumerator.GetDevice(((AudioDeviceInfo)DestinationDeviceComboBox.SelectedItem).Id);
                
                var sourceFormat = sourceDevice.AudioClient.MixFormat;
                var destFormat = destDevice.AudioClient.MixFormat;

                if (sourceFormat.SampleRate == destFormat.SampleRate &&
                    sourceFormat.BitsPerSample == destFormat.BitsPerSample &&
                    sourceFormat.Channels == destFormat.Channels)
                {
                    FormatsMatchText.Text = "YES - Formats match";
                    FormatsMatchText.Foreground = System.Windows.Media.Brushes.Green;
                }
                else
                {
                    FormatsMatchText.Text = "NO - Format conversion needed";
                    FormatsMatchText.Foreground = System.Windows.Media.Brushes.Red;
                }
            }
            catch (Exception ex)
            {
                FormatsMatchText.Text = "Error checking formats";
                Log($"Error comparing formats: {ex.Message}");
            }
        }

        private void OnTestCaptureClick(object sender, RoutedEventArgs e)
        {
            StopTest();
            
            if (SourceDeviceComboBox.SelectedItem is not AudioDeviceInfo device)
            {
                Log("ERROR: No source device selected");
                return;
            }

            try
            {
                Log($"Starting CAPTURE-ONLY test on {device.Name}");
                var enumerator = new MMDeviceEnumerator();
                var mmDevice = enumerator.GetDevice(device.Id);
                
                _capture = new WasapiLoopbackCapture(mmDevice);
                _capture.DataAvailable += (s, args) =>
                {
                    try
                    {
                        // Calculate audio level
                        float max = 0;
                        
                        // Simple level calculation for 16-bit audio
                        for (int i = 0; i < args.BytesRecorded; i += 2)
                        {
                            if (i + 1 < args.BytesRecorded)
                            {
                                short sample = (short)(args.Buffer[i] | (args.Buffer[i + 1] << 8));
                                float sample32 = sample / 32768f;
                                if (Math.Abs(sample32) > max) max = Math.Abs(sample32);
                            }
                        }
                        _lastCaptureLevel = max * 100;
                        
                        // Only log occasionally to avoid flooding
                        if (DateTime.Now.Millisecond < 100)
                        {
                            Dispatcher.BeginInvoke(() =>
                            {
                                Log($"Capture: {args.BytesRecorded} bytes, Level: {_lastCaptureLevel:F1}%");
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.BeginInvoke(() => Log($"Capture error: {ex.Message}"));
                    }
                };
                
                _capture.StartRecording();
                Log("Capture started - play some audio to see levels");
                
                StartMeterTimer();
                SetTestButtons(true);
            }
            catch (Exception ex)
            {
                Log($"ERROR starting capture: {ex.Message}");
            }
        }

        private void OnTestPlaybackClick(object sender, RoutedEventArgs e)
        {
            StopTest();
            
            if (DestinationDeviceComboBox.SelectedItem is not AudioDeviceInfo device)
            {
                Log("ERROR: No destination device selected");
                return;
            }

            try
            {
                Log($"Starting PLAYBACK-ONLY test on {device.Name}");
                var enumerator = new MMDeviceEnumerator();
                var mmDevice = enumerator.GetDevice(device.Id);
                var format = mmDevice.AudioClient.MixFormat;
                
                Log($"Device format: {format.SampleRate}Hz, {format.BitsPerSample}-bit, {format.Channels} channels");
                
                // Create a test tone buffer with explicit format
                _buffer = new BufferedWaveProvider(format)
                {
                    BufferDuration = TimeSpan.FromSeconds(2),
                    DiscardOnBufferOverflow = true
                };
                
                // Create playback with event handlers for debugging
                _playback = new WasapiOut(mmDevice, AudioClientShareMode.Shared, false, 100);
                _playback.PlaybackStopped += (s, args) =>
                {
                    if (args.Exception != null)
                    {
                        Dispatcher.BeginInvoke(() => Log($"Playback stopped with error: {args.Exception.Message}"));
                    }
                    else
                    {
                        Dispatcher.BeginInvoke(() => Log("Playback stopped normally"));
                    }
                };
                
                _playback.Init(_buffer);
                _playback.Play();
                
                Log("Playback initialized and started");
                
                // Generate test tone
                try
                {
                    GenerateTestTone(format);
                    Log("Test tone generated and added to buffer");
                }
                catch (Exception toneEx)
                {
                    Log($"ERROR generating test tone: {toneEx.Message}");
                    Log($"Stack trace: {toneEx.StackTrace}");
                }
                
                SetTestButtons(true);
            }
            catch (Exception ex)
            {
                Log($"ERROR starting playback: {ex.Message}");
                Log($"Stack trace: {ex.StackTrace}");
            }
        }

        private void GenerateTestTone(WaveFormat format)
        {
            Log($"Generating test tone for format: {format.SampleRate}Hz, {format.BitsPerSample}-bit, {format.Channels}ch, Encoding: {format.Encoding}");
            
            // Generate a 1kHz sine wave
            int sampleRate = format.SampleRate;
            int channels = format.Channels;
            int bitsPerSample = format.BitsPerSample;
            int samples = sampleRate * 2; // 2 seconds
            
            // Check if format is IEEE float
            bool isFloat = format.Encoding == WaveFormatEncoding.IeeeFloat;
            
            if (bitsPerSample == 32 && isFloat)
            {
                // 32-bit float format
                Log("Using 32-bit float format");
                byte[] buffer = new byte[samples * channels * 4]; // 32-bit = 4 bytes
                
                for (int n = 0; n < samples; n++)
                {
                    double t = (double)n / sampleRate;
                    float sample = (float)(Math.Sin(2 * Math.PI * 1000 * t) * 0.5); // 0.5 amplitude
                    
                    for (int ch = 0; ch < channels; ch++)
                    {
                        int offset = (n * channels + ch) * 4;
                        byte[] bytes = BitConverter.GetBytes(sample);
                        buffer[offset] = bytes[0];
                        buffer[offset + 1] = bytes[1];
                        buffer[offset + 2] = bytes[2];
                        buffer[offset + 3] = bytes[3];
                    }
                }
                
                if (_buffer != null)
                {
                    _buffer.AddSamples(buffer, 0, buffer.Length);
                    Log($"Generated {buffer.Length} bytes of 32-bit float test tone");
                }
            }
            else if (bitsPerSample == 32 && !isFloat)
            {
                // 32-bit PCM integer format (Extensible)
                Log("Using 32-bit PCM integer format");
                byte[] buffer = new byte[samples * channels * 4]; // 32-bit = 4 bytes
                
                for (int n = 0; n < samples; n++)
                {
                    double t = (double)n / sampleRate;
                    // 32-bit PCM uses full 32-bit range
                    int sample = (int)(Math.Sin(2 * Math.PI * 1000 * t) * 2147483647 * 0.1); // 0.1 amplitude to avoid clipping
                    
                    for (int ch = 0; ch < channels; ch++)
                    {
                        int offset = (n * channels + ch) * 4;
                        byte[] bytes = BitConverter.GetBytes(sample);
                        buffer[offset] = bytes[0];
                        buffer[offset + 1] = bytes[1];
                        buffer[offset + 2] = bytes[2];
                        buffer[offset + 3] = bytes[3];
                    }
                }
                
                if (_buffer != null)
                {
                    _buffer.AddSamples(buffer, 0, buffer.Length);
                    Log($"Generated {buffer.Length} bytes of 32-bit PCM test tone");
                }
            }
            else if (bitsPerSample == 16)
            {
                // 16-bit PCM format
                Log($"Using 16-bit PCM format");
                byte[] buffer = new byte[samples * channels * 2]; // 16-bit = 2 bytes
                
                for (int n = 0; n < samples; n++)
                {
                    double t = (double)n / sampleRate;
                    short sample = (short)(Math.Sin(2 * Math.PI * 1000 * t) * 32767 * 0.5);
                    
                    for (int ch = 0; ch < channels; ch++)
                    {
                        int offset = (n * channels + ch) * 2;
                        buffer[offset] = (byte)(sample & 0xFF);
                        buffer[offset + 1] = (byte)((sample >> 8) & 0xFF);
                    }
                }
                
                if (_buffer != null)
                {
                    _buffer.AddSamples(buffer, 0, buffer.Length);
                    Log($"Generated {buffer.Length} bytes of 16-bit PCM test tone");
                }
            }
            else
            {
                Log($"WARNING: Unsupported format - bitsPerSample={bitsPerSample}, encoding={format.Encoding}");
            }
        }

        private void OnTestFullDupClick(object sender, RoutedEventArgs e)
        {
            StopTest();
            
            if (SourceDeviceComboBox.SelectedItem is not AudioDeviceInfo sourceDevice ||
                DestinationDeviceComboBox.SelectedItem is not AudioDeviceInfo destDevice)
            {
                Log("ERROR: Select both source and destination devices");
                return;
            }

            try
            {
                Log($"Starting FULL DUPLICATION test");
                Log($"  Source: {sourceDevice.Name}");
                Log($"  Destination: {destDevice.Name}");
                
                var enumerator = new MMDeviceEnumerator();
                var sourceMmDevice = enumerator.GetDevice(sourceDevice.Id);
                var destMmDevice = enumerator.GetDevice(destDevice.Id);
                var destFormat = destMmDevice.AudioClient.MixFormat;
                var sourceFormat = sourceMmDevice.AudioClient.MixFormat;
                
                Log($"  Source format: {sourceFormat.SampleRate}Hz, {sourceFormat.BitsPerSample}-bit, {sourceFormat.Channels}ch");
                Log($"  Dest format: {destFormat.SampleRate}Hz, {destFormat.BitsPerSample}-bit, {destFormat.Channels}ch");
                
                // Setup playback with destination format
                _buffer = new BufferedWaveProvider(destFormat)
                {
                    BufferDuration = TimeSpan.FromSeconds(2),
                    DiscardOnBufferOverflow = true
                };
                
                _playback = new WasapiOut(destMmDevice, AudioClientShareMode.Shared, false, 100);
                _playback.Init(_buffer);
                _playback.Play();
                
                Log("Playback initialized");
                
                // Setup capture - use source device's format
                _capture = new WasapiLoopbackCapture(sourceMmDevice);
                Log("Capture initialized");
                _capture.DataAvailable += (s, args) =>
                {
                    try
                    {
                        // Calculate level
                        float max = 0;
                        for (int i = 0; i < args.BytesRecorded; i += 2)
                        {
                            if (i + 1 < args.BytesRecorded)
                            {
                                short sample = (short)(args.Buffer[i] | (args.Buffer[i + 1] << 8));
                                float sample32 = sample / 32768f;
                                if (Math.Abs(sample32) > max) max = Math.Abs(sample32);
                            }
                        }
                        _lastCaptureLevel = max * 100;
                        
                        // Copy to playback buffer only if formats match
                        if (_buffer != null && args.BytesRecorded > 0)
                        {
                            try
                            {
                                var buffer = new byte[args.BytesRecorded];
                                Buffer.BlockCopy(args.Buffer, 0, buffer, 0, args.BytesRecorded);
                                _buffer.AddSamples(buffer, 0, buffer.Length);
                            }
                            catch (Exception bufEx)
                            {
                                Dispatcher.BeginInvoke(() => Log($"Buffer error: {bufEx.Message}"));
                            }
                        }
                        
                        _lastPlaybackLevel = _lastCaptureLevel;
                        
                        // Only log occasionally to avoid flooding
                        if (DateTime.Now.Millisecond < 100)
                        {
                            Dispatcher.BeginInvoke(() =>
                            {
                                Log($"Dup: {args.BytesRecorded} bytes, Level: {_lastCaptureLevel:F1}%");
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.BeginInvoke(() => Log($"Dup error: {ex.Message}"));
                    }
                };
                
                _capture.StartRecording();
                Log("Full duplication started - audio should now play on destination");
                
                StartMeterTimer();
                SetTestButtons(true);
            }
            catch (Exception ex)
            {
                Log($"ERROR starting full duplication: {ex.Message}");
                Log($"Stack trace: {ex.StackTrace}");
            }
        }

        private void OnStopTestClick(object sender, RoutedEventArgs e)
        {
            StopTest();
            SetTestButtons(false);
        }

        private void StopTest()
        {
            _meterTimer?.Stop();
            
            if (_capture != null)
            {
                try
                {
                    _capture.StopRecording();
                    _capture.Dispose();
                    Log("Capture stopped");
                }
                catch { }
                _capture = null;
            }
            
            if (_playback != null)
            {
                try
                {
                    _playback.Stop();
                    _playback.Dispose();
                    Log("Playback stopped");
                }
                catch { }
                _playback = null;
            }
            
            _buffer = null;
            CaptureMeter.Value = 0;
            PlaybackMeter.Value = 0;
        }

        private void StartMeterTimer()
        {
            _meterTimer = new DispatcherTimer();
            _meterTimer.Interval = TimeSpan.FromMilliseconds(100);
            _meterTimer.Tick += (s, e) =>
            {
                CaptureMeter.Value = _lastCaptureLevel;
                PlaybackMeter.Value = _lastPlaybackLevel;
                _lastCaptureLevel *= 0.9f; // Decay
                _lastPlaybackLevel *= 0.9f;
            };
            _meterTimer.Start();
        }

        private void SetTestButtons(bool testing)
        {
            TestCaptureButton.IsEnabled = !testing;
            TestPlaybackButton.IsEnabled = !testing;
            TestFullDupButton.IsEnabled = !testing;
            StopTestButton.IsEnabled = testing;
        }

        private void Log(string message)
        {
            Dispatcher.Invoke(() =>
            {
                LogTextBox.AppendText($"[{DateTime.Now:HH:mm:ss.fff}] {message}\n");
                LogTextBox.ScrollToEnd();
            });
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            StopTest();
            base.OnClosing(e);
        }
    }
}
