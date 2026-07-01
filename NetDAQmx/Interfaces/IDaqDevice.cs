namespace NetDAQmx;

/// <summary>
/// Represents a connected NI-DAQ device and its I/O operations.
/// Use <see cref="NIDAQ"/> for real hardware; implement this interface
/// with a simulation class for unit testing without hardware.
/// </summary>
public interface IDaqDevice
{
    /// <summary>Device alias as configured in NI-MAX, e.g. <c>"Dev1"</c>.</summary>
    string DeviceAlias { get; }

    // ── Device management ─────────────────────────────────────────────────────

    /// <summary>
    /// Returns <see langword="true"/> if the device supports hardware sample-clock timing.
    /// Always <see langword="false"/> for NI USB-6008 AI/AO; the counter (ctr0) supports it.
    /// </summary>
    bool SupportsHardwareTiming();

    /// <summary>Resets the device, clearing all tasks and restoring power-on defaults.</summary>
    void ResetDevice();

    /// <summary>Returns the number of digital I/O lines on the specified port.</summary>
    uint GetPortWidth(byte port);

    // ── Digital output ────────────────────────────────────────────────────────

    /// <summary>
    /// Drives a single digital output line high (<see langword="true"/>) or low (<see langword="false"/>).
    /// </summary>
    void WriteDigitalOutput(byte port, uint channel, bool value);

    /// <summary>Writes all lines of a port at once as a byte mask.</summary>
    void WriteDigitalPort(byte port, byte data);

    // ── Digital input ─────────────────────────────────────────────────────────

    /// <summary>
    /// Reads a single digital input line.
    /// Returns <see langword="true"/> when the line is high.
    /// </summary>
    bool ReadDigitalInput(byte port, uint channel);

    /// <summary>Reads all lines of a port at once, returned as a byte.</summary>
    byte ReadDigitalPort(byte port);

    // ── Analog input ──────────────────────────────────────────────────────────

    /// <summary>Reads a single voltage sample from an analog input channel.</summary>
    double ReadAnalogInput(uint channel, double minValue, double maxValue,
        double timeout = DaqDefaults.TimeoutSeconds);

    /// <summary>
    /// Fills <paramref name="buffer"/> with voltage samples and returns their average.
    /// </summary>
    double ReadAnalogInput(uint channel, double minValue, double maxValue,
        double[] buffer, double timeout = DaqDefaults.TimeoutSeconds);

    // ── Analog output ─────────────────────────────────────────────────────────

    /// <summary>Writes a voltage to an analog output channel.</summary>
    void WriteAnalogOutput(uint channel, double value,
        double minVal = 0.0, double maxVal = 5.0,
        double timeout = DaqDefaults.TimeoutSeconds);

    // ── Counter ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a counter input task, reads the rising-edge count, then clears the task.
    /// The counter resets to zero on each call because a fresh task is created.
    /// For continuous counting, manage a <see cref="Helpers.DaqTask"/> directly.
    /// </summary>
    uint ReadEdgeCount(string counterChannel,
        double timeout = DaqDefaults.TimeoutSeconds);

    // ── Async variants ────────────────────────────────────────────────────────

    /// <inheritdoc cref="ReadAnalogInput(uint,double,double,double)"/>
    Task<double> ReadAnalogInputAsync(uint channel, double minValue, double maxValue,
        double timeout = DaqDefaults.TimeoutSeconds, CancellationToken ct = default);

    /// <inheritdoc cref="WriteAnalogOutput"/>
    Task WriteAnalogOutputAsync(uint channel, double value,
        double minVal = 0.0, double maxVal = 5.0,
        double timeout = DaqDefaults.TimeoutSeconds, CancellationToken ct = default);

    /// <inheritdoc cref="ReadDigitalInput"/>
    Task<bool> ReadDigitalInputAsync(byte port, uint channel, CancellationToken ct = default);

    /// <inheritdoc cref="WriteDigitalOutput"/>
    Task WriteDigitalOutputAsync(byte port, uint channel, bool value, CancellationToken ct = default);

    /// <inheritdoc cref="ReadDigitalPort"/>
    Task<byte> ReadDigitalPortAsync(byte port, CancellationToken ct = default);

    /// <inheritdoc cref="WriteDigitalPort"/>
    Task WriteDigitalPortAsync(byte port, byte data, CancellationToken ct = default);

    /// <inheritdoc cref="ReadEdgeCount"/>
    Task<uint> ReadEdgeCountAsync(string counterChannel,
        double timeout = DaqDefaults.TimeoutSeconds, CancellationToken ct = default);
}
