using System;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;

namespace FileDissector.Domain.FileHandling
{
    public class FileTailer : IDisposable
    {
        private readonly IDisposable _cleanup;

        public FileTailer(FileInfo file, IObservable<string> textToMatch, IObservable<ScrollRequest> scrollRequest)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));
            if (textToMatch == null) throw new ArgumentNullException(nameof(textToMatch));
            if (scrollRequest == null) throw new ArgumentNullException(nameof(scrollRequest));

            // create list of lines which contain the observable text
            var matchedLines = textToMatch
                .Select(searchText =>
                {
                    Func<string, bool> predicate = null;
                    if (!string.IsNullOrEmpty(searchText))
                    {
                        // TODO: for now we use case insensitive search but we need to update it later on 
                        predicate = s => s.Contains(searchText, StringComparison.OrdinalIgnoreCase);
                    }

                    // todo: probably not the most efficient implementation to start reading the file all over again but works for now
                    return file.WatchFile().ScanFile(predicate);
                })
                .Switch()
                .Replay(1) // we use a replay here because we want to create a hot observable that all the subscribers share a single instance of
                .RefCount();

            MatchedLinesCount = matchedLines.Select(x => x.MatchingLines.Length);

            // count of lines
            TotalLinesCount = matchedLines.Select(x => x.TotalLines);

            var lines = new SourceList<Line>(); // this is where the DynamicData magic happens!
            Lines = lines.AsObservableList();   // convert the sourceList to observableList

            // Dynamically combine lines requested by the consumer with the lines which exist in the file. This enables
            // proper virtualization of the file
            var scroller = matchedLines
                .CombineLatest(scrollRequest, (matched, request) => new { matched, request})
                .Subscribe(x =>
                {
                    var mode = x.request.Mode;
                    var pageSize = x.request.PageSize;

                    var endOfTail = x.matched.EndOfTail;
                    var isInitial = x.matched.Index == 0;
                    var allLines = x.matched.MatchingLines;
                    var previousPage = lines.Items.Select(l => l.Number).ToArray();
                    
                    // if tailing, take the end only
                    // otherwise take the page size and start index from the request
                    var currentPage = (mode == ScrollingMode.Tail
                        ? allLines.Skip(allLines.Length - pageSize).ToArray()
                        : allLines.Skip(x.request.FirstIndex - 1).Take(pageSize)).ToArray();

                    var added = currentPage.Except(previousPage).ToArray();
                    var removed = previousPage.Except(currentPage).ToArray();

                    // read new lines from the file
                    var addedLines = file.ReadLines(added, i => !isInitial && i > endOfTail).ToArray();
                    // get old lines from the current collection
                    var removedLines = lines.Items.Where(l => removed.Contains(l.Number)).ToArray();

                    // finally relect changes in the list
                    lines.Edit(innerList =>
                    {
                        innerList.RemoveMany(removedLines);
                        innerList.AddRange(addedLines);
                    });
                });

            _cleanup = new CompositeDisposable(Lines, scroller, lines);
        }

        public IObservable<int> TotalLinesCount { get; }
        public IObservable<int> MatchedLinesCount { get; }
        public IObservableList<Line> Lines { get; }

        public void Dispose()
        {
            _cleanup.Dispose();
        }
    }
}
