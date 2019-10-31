using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using FileDissector.Domain.Infrastructure;

namespace FileDissector.Domain.FileHandling
{
    public static class FileInfoEx
    {
        /// <summary>
        /// Coutns the number of lines in the file which matches the specified predicate.
        /// <remarks>
        /// If no predicate is supplied counts all the lines
        /// </remarks>
        /// </summary>
        /// <param name="file">The <see cref="FileInfo"/> of the target file.</param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static IObservable<int> CountLines(this FileInfo file, Func<string, bool> predicate = null)
        {
            return Observable.Create<int>(observer =>
            {
                var stream = File.Open(file.FullName, FileMode.Open, FileAccess.Read,
                    FileShare.Delete | FileShare.ReadWrite);
                stream.Seek(0, SeekOrigin.Begin);

                var reader = new StreamReader(stream);

                int CountToEnd()
                {
                    var i = 0;
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (predicate == null)
                        {
                            i++;
                        }
                        else
                        {
                            if (predicate(line))
                            {
                                i++;
                            }
                        }
                    }

                    return i;
                }

                var monitor = file.WatchFile()
                    .Where(e => e.ChangeType == WatcherChangeTypes.Changed)
                    .ToUnit()
                    .StartWithUnit()
                    .Scan(CountToEnd(), (total, _) => total + CountToEnd())
                    .SubscribeSafe(observer);

                return Disposable.Create(() =>
                {
                    monitor.Dispose();
                    stream.Close();
                    stream.Dispose();
                    reader.Close();
                    reader.Dispose();
                });
            });
        }

        /// <summary>
        /// Produces an observable array of lines in the file which matches the specified predicate.
        /// <remarks>
        /// if no predicate is supplied all lines are returned
        /// </remarks>
        /// </summary>
        /// <param name="file">The <see cref="FileInfo"/> of the target file.</param>
        /// <param name="predicate">The predicate</param>
        /// <param name="endOfTailChanged">The end of tail changed</param>
        /// <returns></returns>
        public static IObservable<int[]> ScanLineNumbers(this FileInfo file, Func<string, bool> predicate = null, Action<int> endOfTailChanged = null)
        {
            return Observable.Create<int[]>(observer =>
            {
                var stream = File.Open(file.FullName, FileMode.Open, FileAccess.Read,
                    FileShare.Delete | FileShare.ReadWrite);
                stream.Seek(0, SeekOrigin.Begin);

                var reader = new StreamReader(stream);

                string line;

                var monitor = file.WatchFile()
                    .Where(e => e.ChangeType == WatcherChangeTypes.Changed)
                    .ToUnit()
                    .StartWithUnit()
                    .Scan(Tuple.Create(new ImmutableList<int>(), 0), (state, _) =>
                    {
                        var i = state.Item2;
                        var newItems = new List<int>();

                        // notify
                        endOfTailChanged?.Invoke(i);

                        while ((line = reader.ReadLine()) != null)
                        {
                            if (predicate == null)
                            {
                                i++;
                                newItems.Add(i);
                            }
                            else
                            {
                                i++;
                                if (predicate(line))
                                {
                                    newItems.Add(i);
                                }
                            }
                        }

                        var result = state.Item1.Add(newItems.ToArray());
                        return Tuple.Create(result, i);
                    }).Select(tuple => tuple.Item1.Data)
                    .SubscribeSafe(observer);

                return Disposable.Create(() =>
                {
                    monitor.Dispose();
                    stream.Close();
                    stream.Dispose();
                    reader.Close();
                    reader.Dispose();
                });
            });
        }
        public static IObservable<FileSystemEventArgs> WatchFile(this FileInfo file)
        {
            if (file.DirectoryName == null)
            {
                throw new ArgumentException("provided file info does not have a directory");
            }
            return Observable.Create<FileSystemEventArgs>(observer =>
            {
                var watcher = new FileSystemWatcher(file.DirectoryName, file.Name)
                {
                    EnableRaisingEvents = true
                };

                var changed = Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
                        h => watcher.Changed += h,
                        h => watcher.Changed -= h)
                    .Select(ev => ev.EventArgs);

                var deleted = Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
                        h => watcher.Deleted += h,
                        h => watcher.Deleted -= h)
                    .Select(ev => ev.EventArgs);

                var created = Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
                        h => watcher.Created += h,
                        h => watcher.Created -= h)
                    .Select(ev => ev.EventArgs);


                var renamed = Observable.FromEventPattern<RenamedEventHandler, RenamedEventArgs>(
                        h => watcher.Renamed += h,
                        h => watcher.Renamed -= h)
                    .Select(ev => ev.EventArgs);

                return new CompositeDisposable(watcher,
                    changed.Merge(deleted).Merge(created).Merge(renamed).SubscribeSafe(observer));
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

        public static IObservable<int[]> ScanLineNumbers(this FileInfo source, IObservable<string> textToMatch)
        {
            
            return textToMatch
                .Select(searchText =>
                {
                    Func<string, bool> predicate = null;
                    if (!string.IsNullOrEmpty(searchText))
                    {
                        predicate = s => s.Contains(searchText, StringComparison.OrdinalIgnoreCase);
                    }
                    return source.ScanLineNumbers(predicate);
                }).Switch();
        }
    }
}
