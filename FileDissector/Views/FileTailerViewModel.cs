using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using FileDissector.Domain.FileHandling;
using FileDissector.Domain.Infrastructure;
using FileDissector.Infrastructure;

namespace FileDissector.Views
{
    public class FileTailerViewModel : AbstractNotifyPropertyChanged, IDisposable
    {
        private readonly IDisposable _cleanup;
        private readonly ReadOnlyObservableCollection<LineProxy> _data;
        private string _searchText;
        private string _lineCountText;
        private bool _tailing;

        public FileTailerViewModel(ILogger logger, ISchedulerProvider schedulerProvider, FileInfo fileInfo)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            if (schedulerProvider == null) throw new ArgumentNullException(nameof(schedulerProvider));

            File = fileInfo.FullName;
            Tailing = true;

            var tailer = new FileTailer(
                fileInfo, 
                this.WhenValueChanged(vm => vm.SearchText).Throttle(TimeSpan.FromMilliseconds(125)), 
                Observable.Return(new ScrollRequest(40)));

            var lineCounter = tailer
                .TotalLines
                .CombineLatest(tailer.MatchedLines, (total, matched) =>
                    total == matched
                        ? $"File has {total:#,###} lines"
                        : $"Showing {matched:#,###} of {total:#,###} lines")
                .Subscribe(text => LineCountText = text);
                


            var loader = tailer.Lines.Connect()
                .Buffer(TimeSpan.FromMilliseconds(125)).FlattenBufferResult()
                .Transform(line => new LineProxy(line))
                .Sort(SortExpressionComparer<LineProxy>.Ascending(proxy => proxy.Number))
                .ObserveOn(schedulerProvider.MainThread)
                .Bind(out _data)
                .Do(_ => AutoScroller.ScrollToEnd())
                .Subscribe(a => logger.Info(a.Adds.ToString()), ex => logger.Error(ex, "Opps"));

            _cleanup = new CompositeDisposable(tailer, lineCounter, loader);
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

        public bool Tailing
        {
            get => _tailing;
            set => SetAndRaise(ref _tailing, value);
        }

        public void Dispose()
        {
            _cleanup.Dispose();
        }
    }
}
