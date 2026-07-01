namespace NetDAQmx.Helpers;

/// <summary>
/// Wraps a DAQmx task handle as an <see cref="IDisposable"/> resource.
/// Always use inside a <c>using</c> statement, or call <see cref="IDisposable.Dispose"/> explicitly.
/// A finalizer ensures the native handle is released even if disposal is skipped.
/// </summary>
public class DaqTask : IDaqTask
{
    internal IntPtr handle;
    private bool _disposed;

    /// <summary>Creates a DAQmx task with an optional name.</summary>
    /// <param name="taskName">Name assigned to the task; empty string is valid.</param>
    public DaqTask(string taskName = "")
    {
        var status = DllWrapper.DAQmxCreateTask(taskName, out handle);
        NIDAQ.ThrowError(status);
    }

    /// <summary>Commits resources and transitions the task to the running state.</summary>
    public void Start()
    {
        var status = DllWrapper.DAQmxStartTask(handle);
        NIDAQ.ThrowError(status);
    }

    /// <summary>
    /// Stops the task, releasing hardware resources while preserving the
    /// channel and timing configuration for a future <see cref="Start"/>.
    /// </summary>
    public void Stop()
    {
        var status = DllWrapper.DAQmxStopTask(handle);
        NIDAQ.ThrowError(status);
    }

    /// <summary>Ensures the native task handle is released if <see cref="IDisposable.Dispose"/> was not called.</summary>
    ~DaqTask() => Dispose(false);

    private void Dispose(bool disposing)
    {
        if (!_disposed && handle != IntPtr.Zero)
        {
            int status = DllWrapper.DAQmxClearTask(handle);
            handle = IntPtr.Zero;
            _disposed = true;
            // Never throw from the finalizer path — it would terminate the process.
            if (disposing)
                NIDAQ.ThrowError(status);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
