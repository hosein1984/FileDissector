using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.ExceptionServices;
using DynamicData;
using DynamicData.Binding;
using FileDissector.Domain.FileHandling;
using FileDissector.Domain.Infrastructure;
using FileDissector.Infrastructure;

namespace FileDissector.Views
{
    public class FileTailerViewModel : AbstractNotifyPropertyChanged, IDisposable, IScrollReceiver
    {
        private readonly IDisposable _cleanup;
        private readonly ReadOnlyObservableCollection<LineProxy> _data;
        private readonly ISubject<ScrollValues> _userScrollRequested = new Subject<ScrollValues>();
        private string _searchText;
        private string _lineCountText;
        private bool _autoTail;
        private int _firstrow;
        private int _matchedLineCount;
        private int _matchedLinesCount;

        public FileTailerViewModel(ILogger logger, ISchedulerProvider schedulerProvider, FileInfo fileInfo)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            if (schedulerProvider == null) throw new ArgumentNullException(nameof(schedulerProvider));
            if (fileInfo == null) throw new ArgumentNullException(nameof(fileInfo));

            File = fileInfo.FullName;
            AutoTail = true;

            var filterRequest = this.WhenValueChanged(vm => vm.SearchText).Throttle(TimeSpan.FromMilliseconds(125));
            var autoTail = this.WhenValueChanged(vm => vm.AutoTail)
                .CombineLatest(_userScrollRequested, (auto, user) =>
                    auto
                        ? new ScrollRequest(user.Rows)
                        : new ScrollRequest(user.Rows, user.FirstIndex + 1))
                .DistinctUntilChanged();

            var tailer = new FileTailer(fileInfo, filterRequest, autoTail);

            var lineCounter = tailer
                .TotalLines
                .CombineLatest(tailer.MatchedLines, (total, matched) =>
                    total == matched
                        ? $"File has {total:#,###} lines"
                        : $"Showing {matched:#,###} of {total:#,###} lines")
                .Subscribe(text => LineCountText = text);
                
            // load lines into observable collection
            var loader = tailer.Lines.Connect()
                .Buffer(TimeSpan.FromMilliseconds(125)).FlattenBufferResult()
                .Transform(line => new LineProxy(line))
                .Sort(SortExpressionComparer<LineProxy>.Ascending(proxy => proxy.Number))
                .ObserveOn(schedulerProvider.MainThread)
                .Bind(out _data)
                .Do(_ => AutoScroller.ScrollToEnd())
                .Subscribe(a => logger.Info(a.Adds.ToString()), ex => logger.Error(ex, "Opps"));

            // monitor matching lines and start index
            // update local values so the virtual scroll panel can bind to them
            var matchedLinesMonitor = tailer.MatchedLines
                .Subscribe(matched => MatchedLinesCount = matched);

            var firstIndexMonitor = tailer.Lines.Connect()
                .QueryWhenChanged(lines =>
                {
                    // use zero based index rather than line number
                    return lines.Count == 0 ? 0 : lines.Select(l => l.Number).Min() - 1;
                }).Subscribe(first => FirstRow = first - 1);

            _cleanup = new CompositeDisposable(
                tailer, 
                lineCounter, 
                loader,
                matchedLinesMonitor,
                firstIndexMonitor,
                Disposable.Create(() => _userScrollRequested.OnCompleted()));
        }

        public int MatchedLinesCount
        {
            get => _matchedLinesCount;
            set => SetAndRaise(ref _matchedLinesCount, value);
        }

        void IScrollReceiver.RequestChange(ScrollValues values)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));

            _userScrollRequested.OnNext(values);
        }

        public int FirstRow
        {
            get => _firstrow;
            set => SetAndRaise(ref _firstrow, value);
        }

        public string File { get; }

        public AutoScroller AutoScroller { get; } = new AutoScroller();

        public ReadOnlyObservableCollection<LineProxy> Lines => _data;

        public string SearchText
        {
            get => _searchText;
            set => SetAndRaise(ref _searchText, value);
        }

        public string LineCountText
        {
            get => _lineCountText;
            set => SetAndRaise(ref _lineCountText, value);
        }

        public bool AutoTail
        {
            get => _autoTail;
            set => SetAndRaise(ref _autoTail, value);
        }

        public void Dispose()
        {
            _cleanup.Dispose();
        }

        
    }
}
