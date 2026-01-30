using NAudio.Wave;

namespace DuoAudio.Services
{
    /// <summary>
    /// Custom WaveProvider that reads audio data directly from an AudioRingBuffer.
    /// Replaces NAudio's BufferedWaveProvider for zero-copy access to ring buffer data.
    /// </summary>
    public class RingBufferWaveProvider : IWaveProvider
    {
        private readonly AudioRingBuffer _ringBuffer;
        private readonly WaveFormat _waveFormat;
        private readonly byte[] _silenceBuffer;

        /// <summary>
        /// Gets the wave format of the audio data.
        /// </summary>
        public WaveFormat WaveFormat => _waveFormat;

        /// <summary>
        /// Gets the ring buffer this provider reads from.
        /// </summary>
        public AudioRingBuffer RingBuffer => _ringBuffer;

        /// <summary>
        /// Initializes a new instance of the RingBufferWaveProvider class.
        /// </summary>
        /// <param name="ringBuffer">The ring buffer to read audio data from.</param>
        /// <param name="waveFormat">The wave format of the audio data.</param>
        public RingBufferWaveProvider(AudioRingBuffer ringBuffer, WaveFormat waveFormat)
        {
            _ringBuffer = ringBuffer ?? throw new ArgumentNullException(nameof(ringBuffer));
            _waveFormat = waveFormat ?? throw new ArgumentNullException(nameof(waveFormat));

            // Pre-allocate a silence buffer for when ring buffer is empty
            // This prevents audio glitches during buffer underruns
            int silenceBufferSize = waveFormat.AverageBytesPerSecond / 10; // 100ms of silence
            _silenceBuffer = new byte[silenceBufferSize];
        }

        /// <summary>
        /// Reads audio data from the ring buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read audio data into.</param>
        /// <param name="offset">The offset in the buffer to start writing to.</param>
        /// <param name="count">The number of bytes to read.</param>
        /// <returns>The number of bytes actually read.</returns>
        public int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || offset > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0 || offset + count > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(count));

            if (count == 0)
                return 0;

            // Try to read from ring buffer
            int bytesRead = _ringBuffer.Read(buffer, offset, count);

            // If ring buffer is empty (underrun), fill with silence
            if (bytesRead < count)
            {
                int remainingBytes = count - bytesRead;
                int silenceOffset = 0;

                while (remainingBytes > 0)
                {
                    int bytesToCopy = Math.Min(remainingBytes, _silenceBuffer.Length);
                    Buffer.BlockCopy(_silenceBuffer, 0, buffer, offset + bytesRead + silenceOffset, bytesToCopy);
                    remainingBytes -= bytesToCopy;
                    silenceOffset += bytesToCopy;
                }

                // Log underrun for debugging
                System.Diagnostics.Debug.WriteLine($"RingBuffer underrun: requested {count}, got {bytesRead}, filled {count - bytesRead} with silence");
            }

            return count; // Always return the requested count (filled with silence if needed)
        }

        /// <summary>
        /// Clears the ring buffer.
        /// </summary>
        public void Clear()
        {
            _ringBuffer.Clear();
        }

        /// <summary>
        /// Gets the number of bytes available in the ring buffer.
        /// </summary>
        public int BufferedBytes => _ringBuffer.AvailableBytes;

        /// <summary>
        /// Gets the buffer duration in seconds.
        /// </summary>
        public double BufferedDuration => (double)BufferedBytes / _waveFormat.AverageBytesPerSecond;

        /// <summary>
        /// Gets the buffer utilization as a percentage (0.0 to 1.0).
        /// </summary>
        public double BufferUtilization => _ringBuffer.BufferUtilization;
    }
}
