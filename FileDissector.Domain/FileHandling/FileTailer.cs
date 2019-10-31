﻿using System;
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

        public IObservable<int> TotalLines { get; }
        public IObservable<int[]> MatchedLines { get;  }

        public IObservableList<Line> Lines { get; }

        public FileTailer(FileInfo file, IObservable<string> textToMatch, IObservable<ScrollRequest> scrollRequest)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));
            if (textToMatch == null) throw new ArgumentNullException(nameof(textToMatch));

            // create list of lines which contain the observable text
            var matchedLines = textToMatch
                .Select(searchText =>
                {
                    Func<string, bool> predicate = null;
                    if (!string.IsNullOrEmpty(searchText))
                    {
                        predicate = s => s.Contains(searchText, StringComparison.OrdinalIgnoreCase);
                    }

                    // get rid of this end of line state thing (make it part of the scan lines observable)
                    int endOfTail = int.MaxValue;

                    return file.ScanLineNumbers(predicate, eof => endOfTail = eof)
                        .Select((matchingLines, index) => new
                        {
                            matchingLines,
                            isInitial = index == 0,
                            endOfTail
                        });
                }).Switch()
                .Replay(1).RefCount();

            MatchedLines = matchedLines.Select(x => x.matchingLines);

            // count of lines
            TotalLines = file.CountLines();

            var lines = new SourceList<Line>();
            Lines = lines.AsObservableList();

            // Dynamically combine lines requested by the consumer with the lines which exist in the file. This enables
            // proper virtualization of the file
            var scroller = matchedLines
                .CombineLatest(scrollRequest, (matched, request) => new { matched, request})
                .Subscribe(x =>
                {
                    var mode = x.request.Mode;
                    var pageSize = x.request.PageSize;

                    var endOfTail = x.matched.endOfTail;
                    var isInitial = x.matched.isInitial;
                    var allLines = x.matched.matchingLines;
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

        public void Dispose()
        {
            _cleanup.Dispose();
        }
    }
}
