using System.Reactive.Concurrency;

namespace FileDissector.Domain.Infrastructure
{
    public interface ISchedulerProvider
    {
        IScheduler MainThread { get; }
        IScheduler TaskPool { get; }
    }
}
