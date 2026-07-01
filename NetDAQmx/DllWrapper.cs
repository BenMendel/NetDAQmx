using NetDAQmx.Helpers;
using System.Runtime.InteropServices;
using System.Text;

namespace NetDAQmx;

/// <summary>
/// P/Invoke declarations for nicaiu.dll (NI-DAQmx ANSI C library).
/// All function signatures verified against NIDAQmx.h from the NI-DAQ ANSI C Dev package.
/// </summary>
public static class DllWrapper
{
    /// <summary>
    /// Pass as <c>numSampsPerChan</c> to read all available samples.
    /// Equivalent to <c>DAQmx_Val_Auto</c> (-1) in NIDAQmx.h.
    /// </summary>
    public const int DAQmx_Val_Auto = -1;

    /// <summary>
    /// Line grouping parameter for <c>DAQmxCreateDIChan</c> and <c>DAQmxCreateDOChan</c>.
    /// </summary>
    public enum DAQmxLineGrouping
    {
        /// <summary>One virtual channel per physical line. NIDAQmx.h: DAQmx_Val_ChanPerLine (0)</summary>
        ChanPerLine = 0,
        /// <summary>One virtual channel for all lines in the group. NIDAQmx.h: DAQmx_Val_ChanForAllLines (1)</summary>
        ChanForAllLines = 1
    }

    /// <summary>
    /// Sample interleaving layout for multi-channel read/write operations.
    /// </summary>
    public enum DAQmxDataLayout
    {
        /// <summary>Samples grouped by channel (non-interleaved). NIDAQmx.h: DAQmx_Val_GroupByChannel (0)</summary>
        GroupByChannel = 0,
        /// <summary>Samples grouped by scan number (interleaved). NIDAQmx.h: DAQmx_Val_GroupByScanNumber (1)</summary>
        GroupByScanNumber = 1
    }

    /// <summary>
    /// Terminal configuration for analog input channels (<c>DAQmx_AI_TermCfg</c>).
    /// </summary>
    public enum DAQmxAITerminalConfiguration
    {
        /// <summary>NI-DAQmx selects the default at run time. NIDAQmx.h: DAQmx_Val_Cfg_Default (-1)</summary>
        Default = -1,
        /// <summary>Referenced single-ended. NIDAQmx.h: DAQmx_Val_RSE (10083)</summary>
        RSE = 10083,
        /// <summary>Non-referenced single-ended. NIDAQmx.h: DAQmx_Val_NRSE (10078)</summary>
        NRSE = 10078,
        /// <summary>Differential. NIDAQmx.h: DAQmx_Val_Diff (10106)</summary>
        Differential = 10106,
        /// <summary>Pseudodifferential. NIDAQmx.h: DAQmx_Val_PseudoDiff (12529)</summary>
        PseudoDiff = 12529
    }

    /// <summary>
    /// Voltage units for analog output channels (<c>DAQmx_AO_Voltage_Units</c>).
    /// </summary>
    public enum DAQmxAOVoltageUnits
    {
        /// <summary>Volts. NIDAQmx.h: DAQmx_Val_Volts (10348)</summary>
        Volts = 10348,
        /// <summary>Units defined by a custom scale. NIDAQmx.h: DAQmx_Val_FromCustomScale (10065)</summary>
        FromCustomScale = 10065
    }

    /// <summary>
    /// Voltage units for analog input channels (<c>DAQmx_AI_Voltage_Units</c>).
    /// Separate from <see cref="DAQmxAOVoltageUnits"/> for type safety, though the values are the same.
    /// </summary>
    public enum DAQmxAIVoltageUnits
    {
        /// <summary>Volts. NIDAQmx.h: DAQmx_Val_Volts (10348)</summary>
        Volts = 10348,
        /// <summary>Units defined by a custom scale. NIDAQmx.h: DAQmx_Val_FromCustomScale (10065)</summary>
        FromCustomScale = 10065
    }

    /// <summary>
    /// Active edge selection for clocking and triggering.
    /// </summary>
    public enum DAQmxEdge
    {
        /// <summary>Rising edge. NIDAQmx.h: DAQmx_Val_Rising (10280)</summary>
        Rising = 10280,
        /// <summary>Falling edge. NIDAQmx.h: DAQmx_Val_Falling (10171)</summary>
        Falling = 10171
    }

    /// <summary>
    /// Counter counting direction for <c>DAQmxCreateCICountEdgesChan</c>.
    /// </summary>
    public enum DAQmxCountDirection
    {
        /// <summary>Count up. NIDAQmx.h: DAQmx_Val_CountUp (10128)</summary>
        Up = 10128,
        /// <summary>Count down. NIDAQmx.h: DAQmx_Val_CountDown (10124)</summary>
        Down = 10124,
        /// <summary>Direction controlled by an external digital signal. NIDAQmx.h: DAQmx_Val_ExtControlled (10326)</summary>
        ExternallyControlled = 10326
    }

    /// <summary>
    /// Sample mode for <c>DAQmxCfgSampClkTiming</c>.
    /// </summary>
    public enum DAQmxSampleMode
    {
        /// <summary>Acquire or generate a finite number of samples. NIDAQmx.h: DAQmx_Val_FiniteSamps (10178)</summary>
        FiniteSamples = 10178,
        /// <summary>Acquire or generate samples continuously. NIDAQmx.h: DAQmx_Val_ContSamps (10123)</summary>
        ContinuousSamples = 10123
    }

    private const string DllPath = "nicaiu.dll";

    // ── System / device ───────────────────────────────────────────────────────

    [DllImport(DllPath, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    private static extern int DAQmxGetSysDevNames([Out] char[] data, uint bufferSize);

    internal static int DAQmxGetSysDevNames(char[] data)
        => DAQmxGetSysDevNames(data, (uint)data.Length);

    [DllImport(DllPath, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    internal static extern int DAQmxGetPhysicalChanDOPortWidth(string physicalChannel, out uint data);

    [DllImport(DllPath, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    internal static extern int DAQmxResetDevice(string deviceName);

    [DllImport(DllPath, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    internal static extern int DAQmxGetDeviceAttribute(string deviceName, int attribute, IntPtr value);

    /// <summary>Returns whether the device supports hardware-timed analog output sample clocking.</summary>
    [DllImport(DllPath, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    internal static extern int DAQmxGetDevAOSampClkSupported(string device, [MarshalAs(UnmanagedType.Bool)] out bool data);

    /// <summary>Returns whether the device supports hardware-timed counter input sample clocking.</summary>
    [DllImport(DllPath, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    internal static extern int DAQmxGetDevCISampClkSupported(string device, [MarshalAs(UnmanagedType.Bool)] out bool data);

    // ── Error handling ────────────────────────────────────────────────────────

    [DllImport(DllPath, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    internal static extern int DAQmxGetErrorString(int errorCode, StringBuilder errorString, uint bufferSize);

    [DllImport(DllPath, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    internal static extern int DAQmxGetExtendedErrorInfo(StringBuilder errorString, uint bufferSize);

    // ── Task management ───────────────────────────────────────────────────────

    [DllImport(DllPath, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    internal static extern int DAQmxCreateTask(string taskName, out IntPtr taskHandle);

    [DllImport(DllPath, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    internal static extern int DAQmxLoadTask(string taskName, out IntPtr taskHandle);

    [DllImport(DllPath, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    internal static extern int DAQmxStartTask(IntPtr taskHandle);

    [DllImport(DllPath, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    internal static extern int DAQmxStopTask(IntPtr taskHandle);

    [DllImport(DllPath, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    internal static extern int DAQmxClearTask(IntPtr taskHandle);

    [DllImport(DllPath, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    internal static extern int DAQmxGetTaskName(IntPtr taskHandle, StringBuilder data, uint bufferSize);

    [DllImport(DllPath, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    internal static extern int DAQmxGetTaskChannels(IntPtr taskHandle, StringBuilder data, uint bufferSize);

    [DllImport(DllPath, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    internal static extern int DAQmxGetTaskNumChans(IntPtr taskHandle, out uint data);

    // ── Timing ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Configures the sample clock for buffered or continuous acquisition.
    /// Note: NI USB-6008 does NOT support hardware sample clocking for AI/AO.
    /// Call <see cref="NIDAQ.SupportsHardwareTiming"/> before using this function.
    /// </summary>
    [DllImport(DllPath, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    internal static extern int DAQmxCfgSampClkTiming(IntPtr taskHandle, string source,
        double rate, DAQmxEdge activeEdge, DAQmxSampleMode sampleMode, ulong sampsPerChan);

    // ── Triggering ────────────────────────────────────────────────────────────

    /// <summary>Configures a digital edge start trigger.</summary>
    [DllImport(DllPath, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    internal static extern int DAQmxCfgDigEdgeStartTrig(IntPtr taskHandle,
        string triggerSource, int triggerEdge);

    /// <summary>Configures an analog edge start trigger.</summary>
    [DllImport(DllPath, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    internal static extern int DAQmxCfgAnlgEdgeStartTrig(IntPtr taskHandle,
        string triggerSource, int triggerSlope, double triggerLevel);

    // ── Digital output ────────────────────────────────────────────────────────

    [DllImport(DllPath, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    internal static extern int DAQmxCreateDOChan(IntPtr taskHandle, string lines,
        string nameToAssignToLines, DAQmxLineGrouping lineGrouping);

    [DllImport(DllPath, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    internal static extern int DAQmxWriteDigitalLines(IntPtr taskHandle, int numSampsPerChan,
        bool autoStart, double timeout, DAQmxDataLayout dataLayout, byte[] writeArray,
        out int sampsPerChanWritten, IntPtr reserved);

    [DllImport(DllPath, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    private static extern int DAQmxWriteDigitalU8(IntPtr taskHandle, int numSampsPerChan,
        bool autoStart, double timeout, DAQmxDataLayout dataLayout, byte[] writeArray,
        out int sampsPerChanWritten, IntPtr reserved);

    internal static int DAQmxWriteDigitalU8(IntPtr taskHandle, int numSampsPerChan, bool autoStart,
        double timeout, DAQmxDataLayout dataLayout, byte[] writeArray, out int sampsPerChanWritten)
        => DAQmxWriteDigitalU8(taskHandle, numSampsPerChan, autoStart, timeout, dataLayout,
            writeArray, out sampsPerChanWritten, IntPtr.Zero);

    // ── Digital input ─────────────────────────────────────────────────────────

    [DllImport(DllPath, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    internal static extern int DAQmxCreateDIChan(IntPtr taskHandle, string lines,
        string nameToAssignToLines, DAQmxLineGrouping lineGrouping);

    [DllImport(DllPath, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    private static extern int DAQmxReadDigitalLines(IntPtr taskHandle, int numSampsPerChan,
        double timeout, DAQmxDataLayout fillMode, byte[] readArray, uint arraySizeInBytes,
        out int sampsPerChanRead, out int numBytesPerSamp, IntPtr reserved);

    internal static int DAQmxReadDigitalLines(IntPtr taskHandle, int numSampsPerChan, double timeout,
        DAQmxDataLayout fillMode, byte[] readArray, uint arraySizeInBytes,
        out int sampsPerChanRead, out int numBytesPerSamp)
        => DAQmxReadDigitalLines(taskHandle, numSampsPerChan, timeout, fillMode, readArray,
            arraySizeInBytes, out sampsPerChanRead, out numBytesPerSamp, IntPtr.Zero);

    [DllImport(DllPath, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    private static extern int DAQmxReadDigitalU8(IntPtr taskHandle, int numSampsPerChan,
        double timeout, DAQmxDataLayout fillMode, byte[] readArray, uint arraySizeInSamps,
        out int sampsPerChanRead, IntPtr reserved);

    internal static int DAQmxReadDigitalU8(DaqTask task, int numSampsPerChan, double timeout,
        DAQmxDataLayout fillMode, byte[] readArray, uint arraySizeInSamps, out int sampsPerChanRead)
        => DAQmxReadDigitalU8(task.handle, numSampsPerChan, timeout, fillMode, readArray,
            arraySizeInSamps, out sampsPerChanRead, IntPtr.Zero);

    [DllImport(DllPath, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    private static extern int DAQmxReadDigitalScalarU32(IntPtr taskHandle, double timeout,
        out uint value, IntPtr reserved);

    internal static int DAQmxReadDigitalScalarU32(IntPtr taskHandle, double timeout, out uint value)
        => DAQmxReadDigitalScalarU32(taskHandle, timeout, out value, IntPtr.Zero);

    // ── Analog input ──────────────────────────────────────────────────────────

    [DllImport(DllPath, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    internal static extern int DAQmxCreateAIVoltageChan(IntPtr taskHandle, string physicalChannel,
        string nameToAssignToChannel, DAQmxAITerminalConfiguration terminalConfig,
        double minVal, double maxVal, DAQmxAIVoltageUnits units, string? customScaleName = null);

    [DllImport(DllPath, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    private static extern int DAQmxReadAnalogScalarF64(IntPtr taskHandle, double timeout,
        out double value, IntPtr reserved);

    internal static int DAQmxReadAnalogScalarF64(IntPtr taskHandle, double timeout, out double value)
        => DAQmxReadAnalogScalarF64(taskHandle, timeout, out value, IntPtr.Zero);

    [DllImport(DllPath, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    private static extern int DAQmxReadAnalogF64(IntPtr taskHandle, int numSampsPerChan,
        double timeout, DAQmxDataLayout fillMode, double[] readArray, uint arraySizeInSamps,
        out int sampsPerChanRead, IntPtr reserved);

    internal static int DAQmxReadAnalogF64(DaqTask daqTask, int numSampsPerChan, double timeout,
        DAQmxDataLayout fillMode, double[] readArray, uint arraySizeInSamps, out int sampsPerChanRead)
        => DAQmxReadAnalogF64(daqTask.handle, numSampsPerChan, timeout, fillMode, readArray,
            arraySizeInSamps, out sampsPerChanRead, IntPtr.Zero);

    internal static int DAQmxReadAnalogF64(DaqTask daqTask, double timeout,
        DAQmxDataLayout fillMode, double[] readArray, uint arraySizeInSamps,
        out int sampsPerChanRead, int numSampsPerChan = DAQmx_Val_Auto)
        => DAQmxReadAnalogF64(daqTask, numSampsPerChan, timeout, fillMode,
            readArray, arraySizeInSamps, out sampsPerChanRead);

    // ── Analog output ─────────────────────────────────────────────────────────

    [DllImport(DllPath, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    internal static extern int DAQmxCreateAOVoltageChan(IntPtr taskHandle, string physicalChannel,
        string nameToAssignToChannel, double minVal, double maxVal,
        DAQmxAOVoltageUnits units, string? customScaleName = null);

    [DllImport(DllPath, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    private static extern int DAQmxWriteAnalogScalarF64(IntPtr taskHandle, bool autoStart,
        double timeout, double value, IntPtr reserved);

    internal static int DAQmxWriteAnalogScalarF64(IntPtr taskHandle, bool autoStart,
        double timeout, double value)
        => DAQmxWriteAnalogScalarF64(taskHandle, autoStart, timeout, value, IntPtr.Zero);

    // ── Counter input ─────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a counter input channel for edge counting (e.g. Dev1/ctr0).
    /// On NI USB-6008, ctr0 supports up to 5 MHz input.
    /// </summary>
    [DllImport(DllPath, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    internal static extern int DAQmxCreateCICountEdgesChan(IntPtr taskHandle, string counter,
        string nameToAssignToChannel, int edge, uint initialCount, int countDirection);

    [DllImport(DllPath, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    private static extern int DAQmxReadCounterScalarU32(IntPtr taskHandle, double timeout,
        out uint value, IntPtr reserved);

    internal static int DAQmxReadCounterScalarU32(IntPtr taskHandle, double timeout, out uint value)
        => DAQmxReadCounterScalarU32(taskHandle, timeout, out value, IntPtr.Zero);

    [DllImport(DllPath, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    private static extern int DAQmxReadCounterScalarF64(IntPtr taskHandle, double timeout,
        out double value, IntPtr reserved);

    internal static int DAQmxReadCounterScalarF64(IntPtr taskHandle, double timeout, out double value)
        => DAQmxReadCounterScalarF64(taskHandle, timeout, out value, IntPtr.Zero);
}
