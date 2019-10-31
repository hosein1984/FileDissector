﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using FileDissector.Domain.Infrastructure;

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
                .Where(n => n.NotificationType == FileNotificationType.Created)
                .Select(createdNotification =>
                {
                    return Observable.Create<FileScanResult>(observer =>
                    {
                        var stream = File.Open(createdNotification.FullName, FileMode.Open, FileAccess.Read,
                            FileShare.Delete | FileShare.ReadWrite);
                        stream.Seek(0, SeekOrigin.Begin);

                        var reader = new StreamReader(stream);

                        string line;

                        var notifier = source
                            .Where(n => n.NotificationType == FileNotificationType.Changed)
                            .StartWith(createdNotification)
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
                var referesh = refereshPeriod ?? TimeSpan.FromMilliseconds(250);
                scheduler = scheduler ?? Scheduler.Default;

                // todo: create a cool-off period after a poll to account for over running jobs
                Func<IObservable<FileNotification>> poller = () => Observable.Interval(referesh, scheduler)
                    .Scan((FileNotification) null, (state, _) =>
                        state == null
                            ? new FileNotification(file)
                            : new FileNotification(state))
                    .DistinctUntilChanged();

                // in theory, poll merrily away except slow down when there is an error
                return poller()
                    .Catch<FileNotification, Exception>(ex =>
                        Observable.Return(new FileNotification(file, ex))
                            .Concat(poller().DelaySubscription(TimeSpan.FromSeconds(10))))
                    .SubscribeSafe(observer);
            });
        }
        



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
