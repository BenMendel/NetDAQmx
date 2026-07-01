namespace NetDAQmx;

/// <summary>Library-wide defaults for DAQmx operations.</summary>
public static class DaqDefaults
{
    /// <summary>Default read/write timeout (10 seconds).</summary>
    public const double TimeoutSeconds = 10.0;

    /// <summary>
    /// Block indefinitely until the operation completes or an error occurs.
    /// Equivalent to <c>DAQmx_Val_WaitInfinitely</c> (-1) in NIDAQmx.h.
    /// </summary>
    public const double InfiniteTimeout = -1.0;
}
