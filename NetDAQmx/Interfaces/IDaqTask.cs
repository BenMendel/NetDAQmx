namespace NetDAQmx;

/// <summary>
/// Represents a DAQmx task with explicit lifecycle control.
/// Obtained from <see cref="Helpers.DaqTask"/> for direct hardware access.
/// </summary>
public interface IDaqTask : IDisposable
{
    /// <summary>Commits resources and transitions the task to the running state.</summary>
    void Start();

    /// <summary>
    /// Stops the task, releasing hardware resources while preserving the
    /// channel and timing configuration for a future <see cref="Start"/>.
    /// </summary>
    void Stop();
}
