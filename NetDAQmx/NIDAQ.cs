using NetDAQmx.Helpers;
using System.Text;
using static NetDAQmx.DllWrapper;

namespace NetDAQmx;

/// <summary>
/// High-level .NET API for a connected NI-DAQ device.
/// Implements <see cref="IDaqDevice"/> for real hardware access via nicaiu.dll.
/// For unit testing without hardware, implement <see cref="IDaqDevice"/> with a simulation class.
/// </summary>
public class NIDAQ : IDaqDevice
{
    // ── Static helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the aliases of all NI-DAQmx devices currently installed in the system.
    /// </summary>
    public static string[] GetSystemDevices()
    {
        // Use a large buffer; NI-DAQ returns a null-delimited, double-null-terminated string.
        char[] buffer = new char[4096];
        int status = DAQmxGetSysDevNames(buffer);

        if (status != 0)
            return Array.Empty<string>();

        string deviceNames = new string(buffer).TrimEnd('\0');
        if (string.IsNullOrEmpty(deviceNames))
            return Array.Empty<string>();

        return deviceNames.Split('\0', StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Converts a DAQmx status code to a <see cref="DaqException"/>.
    /// No-ops for success (code == 0).
    /// </summary>
    public static void ThrowError(int code)
    {
        if (code == 0)
            return;

        var message = new StringBuilder(2048);
        DAQmxGetErrorString(code, message, (uint)message.Capacity);

        var extended = new StringBuilder(4096);
        DAQmxGetExtendedErrorInfo(extended, (uint)extended.Capacity);

        string msg = message.ToString().Trim();
        if (msg.Length == 0)
            msg = $"DAQmx error code {code}";

        throw new DaqException(code, msg, extended.ToString().Trim());
    }

    // ── Instance ──────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public string DeviceAlias { get; }

    /// <summary>Creates a device handle for the specified NI-MAX device alias.</summary>
    /// <param name="deviceAlias">The device alias as shown in NI-MAX (e.g. <c>"Dev1"</c>).</param>
    public NIDAQ(string deviceAlias = "Dev0")
    {
        DeviceAlias = deviceAlias;
    }

    // ── Device management ─────────────────────────────────────────────────────

    /// <inheritdoc/>
    public bool SupportsHardwareTiming()
    {
        // NI USB-6008 returns false for AO; true for CI (counter). We check AO as the
        // representative test since AI/AO share the same software-timed constraint.
        var status = DAQmxGetDevAOSampClkSupported(DeviceAlias, out bool supported);
        ThrowError(status);
        return supported;
    }

    /// <inheritdoc/>
    public void ResetDevice()
    {
        var status = DAQmxResetDevice(DeviceAlias);
        ThrowError(status);
    }

    /// <inheritdoc/>
    public uint GetPortWidth(byte port)
    {
        var status = DAQmxGetPhysicalChanDOPortWidth($"{DeviceAlias}/port{port}", out uint width);
        ThrowError(status);
        return width;
    }

    // ── Digital output ────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public void WriteDigitalOutput(byte port, uint channel, bool value)
    {
        string identifier = $"{DeviceAlias}/port{port}/line{channel}";
        using var task = new DaqTask();
        var status = DAQmxCreateDOChan(task.handle, identifier, "", DAQmxLineGrouping.ChanPerLine);
        ThrowError(status);
        // value=true → drive high (1); value=false → drive low (0)
        byte[] data = [value ? (byte)1 : (byte)0];
        status = DAQmxWriteDigitalLines(task.handle, 1, true, DaqDefaults.TimeoutSeconds,
            DAQmxDataLayout.GroupByChannel, data, out _, IntPtr.Zero);
        ThrowError(status);
    }

    /// <inheritdoc/>
    public void WriteDigitalPort(byte port, byte data)
    {
        string identifier = $"{DeviceAlias}/port{port}";
        using var task = new DaqTask();
        var status = DAQmxCreateDOChan(task.handle, identifier, "", DAQmxLineGrouping.ChanForAllLines);
        ThrowError(status);
        byte[] dataArray = [data];
        status = DAQmxWriteDigitalU8(task.handle, 1, true, DaqDefaults.TimeoutSeconds,
            DAQmxDataLayout.GroupByChannel, dataArray, out _);
        ThrowError(status);
    }

    // ── Digital input ─────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public bool ReadDigitalInput(byte port, uint channel)
    {
        string identifier = $"{DeviceAlias}/port{port}/line{channel}";
        using var task = new DaqTask();
        var status = DAQmxCreateDIChan(task.handle, identifier, "", DAQmxLineGrouping.ChanPerLine);
        ThrowError(status);
        byte[] data = new byte[8];
        status = DAQmxReadDigitalLines(task.handle, 1, DaqDefaults.TimeoutSeconds,
            DAQmxDataLayout.GroupByChannel, data, (uint)data.Length, out _, out _);
        ThrowError(status);
        return data[0] == 1;
    }

    /// <inheritdoc/>
    public byte ReadDigitalPort(byte port)
    {
        string identifier = $"{DeviceAlias}/port{port}";
        using var task = new DaqTask();
        var status = DAQmxCreateDIChan(task.handle, identifier, "", DAQmxLineGrouping.ChanForAllLines);
        ThrowError(status);
        status = DAQmxReadDigitalScalarU32(task.handle, DaqDefaults.TimeoutSeconds, out uint value);
        ThrowError(status);
        return (byte)value;
    }

    // ── Analog input ──────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public double ReadAnalogInput(uint channel, double minValue, double maxValue,
        double timeout = DaqDefaults.TimeoutSeconds)
    {
        using var task = new DaqTask();
        var status = DAQmxCreateAIVoltageChan(task.handle, $"{DeviceAlias}/ai{channel}", "",
            DAQmxAITerminalConfiguration.RSE, minValue, maxValue, DAQmxAIVoltageUnits.Volts);
        ThrowError(status);
        status = DAQmxReadAnalogScalarF64(task.handle, timeout, out double result);
        ThrowError(status);
        return result;
    }

    /// <inheritdoc/>
    public double ReadAnalogInput(uint channel, double minValue, double maxValue,
        double[] buffer, double timeout = DaqDefaults.TimeoutSeconds)
    {
        using var task = new DaqTask();
        var status = DAQmxCreateAIVoltageChan(task.handle, $"{DeviceAlias}/ai{channel}", "",
            DAQmxAITerminalConfiguration.RSE, minValue, maxValue, DAQmxAIVoltageUnits.Volts);
        ThrowError(status);
        for (int i = 0; i < buffer.Length; i++)
        {
            status = DAQmxReadAnalogScalarF64(task.handle, timeout, out buffer[i]);
            ThrowError(status);
        }
        return buffer.Average();
    }

    // ── Analog output ─────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public void WriteAnalogOutput(uint channel, double value,
        double minVal = 0.0, double maxVal = 5.0,
        double timeout = DaqDefaults.TimeoutSeconds)
    {
        using var task = new DaqTask();
        var status = DAQmxCreateAOVoltageChan(task.handle, $"{DeviceAlias}/ao{channel}", "",
            minVal, maxVal, DAQmxAOVoltageUnits.Volts);
        ThrowError(status);
        status = DAQmxWriteAnalogScalarF64(task.handle, true, timeout, value);
        ThrowError(status);
    }

    // ── Counter ───────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public uint ReadEdgeCount(string counterChannel,
        double timeout = DaqDefaults.TimeoutSeconds)
    {
        using var task = new DaqTask();
        var status = DAQmxCreateCICountEdgesChan(task.handle, counterChannel, "",
            (int)DAQmxEdge.Rising, 0, (int)DAQmxCountDirection.Up);
        ThrowError(status);
        task.Start();
        status = DAQmxReadCounterScalarU32(task.handle, timeout, out uint count);
        task.Stop();
        ThrowError(status);
        return count;
    }

    // ── Async variants ────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public Task<double> ReadAnalogInputAsync(uint channel, double minValue, double maxValue,
        double timeout = DaqDefaults.TimeoutSeconds, CancellationToken ct = default)
        => Task.Run(() => ReadAnalogInput(channel, minValue, maxValue, timeout), ct);

    /// <inheritdoc/>
    public Task WriteAnalogOutputAsync(uint channel, double value,
        double minVal = 0.0, double maxVal = 5.0,
        double timeout = DaqDefaults.TimeoutSeconds, CancellationToken ct = default)
        => Task.Run(() => WriteAnalogOutput(channel, value, minVal, maxVal, timeout), ct);

    /// <inheritdoc/>
    public Task<bool> ReadDigitalInputAsync(byte port, uint channel, CancellationToken ct = default)
        => Task.Run(() => ReadDigitalInput(port, channel), ct);

    /// <inheritdoc/>
    public Task WriteDigitalOutputAsync(byte port, uint channel, bool value, CancellationToken ct = default)
        => Task.Run(() => WriteDigitalOutput(port, channel, value), ct);

    /// <inheritdoc/>
    public Task<byte> ReadDigitalPortAsync(byte port, CancellationToken ct = default)
        => Task.Run(() => ReadDigitalPort(port), ct);

    /// <inheritdoc/>
    public Task WriteDigitalPortAsync(byte port, byte data, CancellationToken ct = default)
        => Task.Run(() => WriteDigitalPort(port, data), ct);

    /// <inheritdoc/>
    public Task<uint> ReadEdgeCountAsync(string counterChannel,
        double timeout = DaqDefaults.TimeoutSeconds, CancellationToken ct = default)
        => Task.Run(() => ReadEdgeCount(counterChannel, timeout), ct);

    // ── Obsolete API (v1.x compatibility) ────────────────────────────────────

    /// <inheritdoc cref="WriteDigitalOutput"/>
    [Obsolete("Use WriteDigitalOutput. Note: the 'close' parameter had relay-specific semantics; " +
              "WriteDigitalOutput(port, channel, !close) is the equivalent call.")]
    public void WriteDOChannel(byte port, uint channel, bool close)
        => WriteDigitalOutput(port, channel, !close);

    /// <inheritdoc cref="ReadDigitalInput"/>
    [Obsolete("Use ReadDigitalInput.")]
    public bool IsLineOpen(byte port, uint channel)
        => ReadDigitalInput(port, channel);

    /// <inheritdoc cref="WriteDigitalPort"/>
    [Obsolete("Use WriteDigitalPort.")]
    public void WritePort(byte port, byte data)
        => WriteDigitalPort(port, data);

    /// <inheritdoc cref="ReadDigitalPort"/>
    [Obsolete("Use ReadDigitalPort.")]
    public byte ReadPort(byte port)
        => ReadDigitalPort(port);

    /// <inheritdoc cref="ReadAnalogInput(uint,double,double,double)"/>
    [Obsolete("Use ReadAnalogInput.")]
    public double GetAnalogInputSingleLine(uint channel, double minValue, double maxValue)
        => ReadAnalogInput(channel, minValue, maxValue);

    /// <inheritdoc cref="ReadAnalogInput(uint,double,double,double[],double)"/>
    [Obsolete("Use ReadAnalogInput with a buffer parameter.")]
    public double GetAnalogInputSingleLine(uint channel, double minValue, double maxValue,
        double[] readArray)
        => ReadAnalogInput(channel, minValue, maxValue, readArray);

    /// <inheritdoc cref="WriteAnalogOutput"/>
    [Obsolete("Use WriteAnalogOutput. The method name was incorrect — this writes analog output, not input.")]
    public void SetAnalogInputValue(uint channel, double value, double minVal = 0, double maxVal = 5)
        => WriteAnalogOutput(channel, value, minVal, maxVal);
}
