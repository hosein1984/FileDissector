using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace FileDissector.Domain.FileHandling
{
    public static class FileInfoEx
    {
        /// <summary>
        /// Produces an observable report of lines in the file which matches the specified predicate. Together with meta data to
        /// assist reading the actual file lines
        /// <remarks>
        /// If no predicate is supplied all lines are returned
        /// </remarks>
        /// </summary>
        /// <param name="source"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static IObservable<FileScanResult> ScanFile(this IObservable<FileNotification> source, Func<string, bool> predicate = null)
        {
            return source
                .Where(n => n.NotificationType == FileNotificationType.Created) // when a created notification type is seen open the file and start reading (and return that observable)
                .Select(createdNotification =>
                {
                    return Observable.Create<FileScanResult>(observer =>
                    {
                        var stream = File.Open(createdNotification.FullName, FileMode.Open, FileAccess.Read,
                            FileShare.Delete | FileShare.ReadWrite);
                        stream.Seek(0, SeekOrigin.Begin);

                        var reader = new StreamReader(stream);

                        string line;

                        var notifier = source // yeah we can reuse the main source in the inner observable!!!
                            .Where(n => n.NotificationType == FileNotificationType.Changed)
                            .StartWith(createdNotification) // start with the created notification so observer see creation notification too
                            .Scan((FileScanResult) null, (state, notification) =>
                            {
                                var count = state?.TotalLines ?? 0;
                                var index = state?.Index ?? 0;
                                var previousCount = count;
                                var previousItems = state?.MatchingLines ?? new int[0];
                                var newItems = new List<int>();

                                while ((line = reader.ReadLine()) != null)
                                {
                                    if (predicate == null)
                                    {
                                        count++;
                                        newItems.Add(count);
                                    }
                                    else
                                    {
                                        count++;
                                        if (predicate(line))
                                        {
                                            newItems.Add(count);
                                        }
                                    }
                                }

                                // combine the two arrays
                                var newLines = new int[previousItems.Length + newItems.Count];
                                previousItems.CopyTo(newLines, 0);
                                newItems.CopyTo(newLines, previousItems.Length);

                                return new FileScanResult(notification, newLines, count, previousCount, index);
                            })
                            .SubscribeSafe(observer);

                        return Disposable.Create(() =>
                        {
                            notifier.Dispose();
                            stream.Close();
                            stream.Dispose();
                            reader.Close();
                            reader.Dispose();
                        });
                    });
                }).Switch();
        }

        /// <summary>
        /// A simpler alternative to the irritatingly useless FileSystemWatcher
        /// </summary>
        /// <param name="file">The file to monitor</param>
        /// <param name="refereshPeriod">The refresh period</param>
        /// <param name="scheduler">The scheduler</param>
        /// <returns></returns>
        public static IObservable<FileNotification> WatchFile(this FileInfo file, TimeSpan? refereshPeriod = null, IScheduler scheduler = null)
        {
            return Observable.Create<FileNotification>(observer =>
            {
                var refreshInterval = refereshPeriod ?? TimeSpan.FromMilliseconds(250);
                scheduler = scheduler ?? Scheduler.Default;

                // todo: create a cool-off period after a poll to account for over running jobs
                IObservable<FileNotification> Poller() => Observable
                    .Interval(refreshInterval, scheduler)
                    .StartWith(0)
                    .Scan((FileNotification) null, (state, _) =>
                        state == null
                            ? new FileNotification(file)   // for the first time when we have no prior data (notifications) about the file
                            : new FileNotification(state)) // for subsequent times when we have prior data about the file
                    .DistinctUntilChanged();

                // in theory, poll merrily away except slow down when there is an error
                return Poller()
                    .Catch<FileNotification, Exception>(ex =>
                        Observable.Return(new FileNotification(file, ex)) // for when exceptions happen while retreving file data
                            .Concat(Poller().DelaySubscription(TimeSpan.FromSeconds(10)))) // create a new subscription with a 10 sec delay
                    .SubscribeSafe(observer);
            });
        }
        


        /// <summary>
        /// Reads the specified lines numbers from the file
        /// </summary>
        /// <param name="source">The file</param>
        /// <param name="lines">Line numbers to load from file</param>
        /// <param name="isEndOfTail">A predicate that show if the line is end of the file or not. If returns true, we set the time for the loaded line to now.</param>
        /// <returns></returns>
        public static IEnumerable<Line> ReadLines(this FileInfo source, int[] lines, Func<int, bool> isEndOfTail = null)
        {
            using (var stream = File.Open(source.FullName, FileMode.Open, FileAccess.Read, FileShare.Delete | FileShare.ReadWrite))
            {
                using (var reader = new StreamReader(stream))
                {
                    string line;
                    int position = 0;
                    while ((line = reader.ReadLine()) != null)
                    {
                        position++;

                        if (lines.Contains(position))
                        {
                            yield return new Line(position, line, isEndOfTail == null ? null : (isEndOfTail(position) ? DateTime.Now : (DateTime?)null));
                        }
                    }
                }
            }
        }
    }
}
