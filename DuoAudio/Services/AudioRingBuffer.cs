namespace DuoAudio.Services
{
    /// <summary>
    /// Thread-safe circular ring buffer for audio data streaming.
    /// Eliminates per-chunk allocations by using a single pre-allocated buffer.
    /// </summary>
    public class AudioRingBuffer
    {
        private readonly byte[] _buffer;
        private int _readPosition;
        private int _writePosition;
        private int _availableBytes;
        private readonly object _lock = new();
        private long _totalBytesWritten;
        private long _totalBytesRead;
        private long _overflowCount;

        /// <summary>
        /// Gets the total capacity of the ring buffer in bytes.
        /// </summary>
        public int Capacity => _buffer.Length;

        /// <summary>
        /// Gets the number of bytes currently available for reading.
        /// </summary>
        public int AvailableBytes
        {
            get
            {
                lock (_lock)
                {
                    return _availableBytes;
                }
            }
        }

        /// <summary>
        /// Gets the number of bytes of free space available for writing.
        /// </summary>
        public int AvailableSpace
        {
            get
            {
                lock (_lock)
                {
                    return Capacity - _availableBytes;
                }
            }
        }

        /// <summary>
        /// Gets the total number of bytes written to the buffer.
        /// </summary>
        public long TotalBytesWritten => _totalBytesWritten;

        /// <summary>
        /// Gets the total number of bytes read from the buffer.
        /// </summary>
        public long TotalBytesRead => _totalBytesRead;

        /// <summary>
        /// Gets the number of overflow events (when buffer was full and data was discarded).
        /// </summary>
        public long OverflowCount => _overflowCount;

        /// <summary>
        /// Initializes a new instance of the AudioRingBuffer class with the specified capacity.
        /// </summary>
        /// <param name="capacity">The capacity of the ring buffer in bytes.</param>
        public AudioRingBuffer(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentException("Capacity must be greater than zero.", nameof(capacity));

            _buffer = new byte[capacity];
            _readPosition = 0;
            _writePosition = 0;
            _availableBytes = 0;
        }

        /// <summary>
        /// Writes data to the ring buffer.
        /// </summary>
        /// <param name="data">The data to write.</param>
        /// <param name="offset">The offset in the data array to start writing from.</param>
        /// <param name="count">The number of bytes to write.</param>
        /// <returns>The number of bytes actually written (may be less than count if buffer is full).</returns>
        public int Write(byte[] data, int offset, int count)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (offset < 0 || offset > data.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0 || offset + count > data.Length)
                throw new ArgumentOutOfRangeException(nameof(count));

            if (count == 0)
                return 0;

            lock (_lock)
            {
                int bytesToWrite = Math.Min(count, AvailableSpace);

                if (bytesToWrite == 0)
                {
                    // Buffer is full, track overflow
                    _overflowCount++;
                    return 0;
                }

                // Write data, handling wrap-around
                int spaceToEnd = Capacity - _writePosition;
                int firstChunk = Math.Min(bytesToWrite, spaceToEnd);

                Buffer.BlockCopy(data, offset, _buffer, _writePosition, firstChunk);

                if (bytesToWrite > firstChunk)
                {
                    // Wrap around and write remaining data
                    int secondChunk = bytesToWrite - firstChunk;
                    Buffer.BlockCopy(data, offset + firstChunk, _buffer, 0, secondChunk);
                }

                _writePosition = (_writePosition + bytesToWrite) % Capacity;
                _availableBytes += bytesToWrite;
                _totalBytesWritten += bytesToWrite;

                return bytesToWrite;
            }
        }

        /// <summary>
        /// Reads data from the ring buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read data into.</param>
        /// <param name="offset">The offset in the buffer to start writing to.</param>
        /// <param name="count">The number of bytes to read.</param>
        /// <returns>The number of bytes actually read (may be less than count if buffer is empty).</returns>
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

            lock (_lock)
            {
                int bytesToRead = Math.Min(count, _availableBytes);

                if (bytesToRead == 0)
                    return 0;

                // Read data, handling wrap-around
                int dataToEnd = Capacity - _readPosition;
                int firstChunk = Math.Min(bytesToRead, dataToEnd);

                Buffer.BlockCopy(_buffer, _readPosition, buffer, offset, firstChunk);

                if (bytesToRead > firstChunk)
                {
                    // Wrap around and read remaining data
                    int secondChunk = bytesToRead - firstChunk;
                    Buffer.BlockCopy(_buffer, 0, buffer, offset + firstChunk, secondChunk);
                }

                _readPosition = (_readPosition + bytesToRead) % Capacity;
                _availableBytes -= bytesToRead;
                _totalBytesRead += bytesToRead;

                return bytesToRead;
            }
        }

        /// <summary>
        /// Clears all data from the ring buffer.
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _readPosition = 0;
                _writePosition = 0;
                _availableBytes = 0;
            }
        }

        /// <summary>
        /// Resets the statistics counters (total bytes written/read, overflow count).
        /// </summary>
        public void ResetStatistics()
        {
            lock (_lock)
            {
                _totalBytesWritten = 0;
                _totalBytesRead = 0;
                _overflowCount = 0;
            }
        }

        /// <summary>
        /// Gets the buffer utilization as a percentage (0.0 to 1.0).
        /// </summary>
        public double BufferUtilization
        {
            get
            {
                lock (_lock)
                {
                    return (double)_availableBytes / Capacity;
                }
            }
        }
    }
}
