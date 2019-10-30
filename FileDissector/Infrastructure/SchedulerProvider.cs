using System.Reactive.Concurrency;
using System.Windows.Threading;
using FileDissector.Domain.Infrastructure;

namespace FileDissector.Infrastructure
{
    public class SchedulerProvider : ISchedulerProvider
    {
        public IScheduler MainThread { get; }
        public IScheduler TaskPool { get; } = TaskPoolScheduler.Default;

        public SchedulerProvider(Dispatcher dispatcher)
        {
            MainThread = new DispatcherScheduler(dispatcher);
        }
    }
}
