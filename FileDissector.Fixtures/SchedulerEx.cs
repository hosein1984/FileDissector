using System;
using Microsoft.Reactive.Testing;

namespace FileDissector.Fixtures
{
    public static class SchedulerEx
    {
        public static void AdvanceBySeconds(this TestScheduler source, int seconds)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            source.AdvanceBy(TimeSpan.FromSeconds(seconds).Ticks);
        }

        public static void AdvanceByMilliSeconds(this TestScheduler source, int milliseconds)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            source.AdvanceBy(TimeSpan.FromMilliseconds(milliseconds).Ticks);
        }

    }
}
