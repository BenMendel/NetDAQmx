namespace NetDAQmx;

/// <summary>
/// Exception thrown when a NI-DAQmx operation returns an error or warning code.
/// </summary>
public sealed class DaqException : Exception
{
    /// <summary>The numeric DAQmx error or warning code returned by nicaiu.dll.</summary>
    public int ErrorCode { get; }

    /// <summary>
    /// Extended diagnostic text from <c>DAQmxGetExtendedErrorInfo</c>,
    /// which includes possible causes and suggested solutions.
    /// </summary>
    public string ExtendedInfo { get; }

    internal DaqException(int errorCode, string message, string extendedInfo)
        : base(message)
    {
        ErrorCode = errorCode;
        ExtendedInfo = extendedInfo;
    }
}
