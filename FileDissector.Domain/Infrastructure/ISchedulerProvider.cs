using System.Reactive.Concurrency;

namespace FileDissector.Domain.Infrastructure
{
    /// <summary>
    /// Adds a layer of abstraction to Scheduler. This way we can test the classes that rely on schedulers
    /// </summary>
    public interface ISchedulerProvider
    {
        IScheduler MainThread { get; }
        IScheduler TaskPool { get; }
    }
}
