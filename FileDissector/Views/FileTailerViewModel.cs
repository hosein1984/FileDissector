using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using FileDissector.Domain.FileHandling;
using FileDissector.Domain.Infrastructure;

namespace FileDissector.Views
{
    public class FileTailerViewModel : AbstractNotifyPropertyChanged, IDisposable
    {
        private readonly IDisposable _cleanup;
        private readonly ReadOnlyObservableCollection<LineProxy> _data;
        private string _searchText;
        private int _totalLines;
        private int _filteredLines;

        public FileTailerViewModel(ILogger logger, ISchedulerProvider schedulerProvider, FileInfo fileInfo)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            if (schedulerProvider == null) throw new ArgumentNullException(nameof(schedulerProvider));

            File = fileInfo.FullName;

            var tailer = new FileTailer(
                fileInfo, 
                this.WhenValueChanged(vm => vm.SearchText), 
                Observable.Return(new ScrollRequest(40)));

            var totalCount = tailer.TotalLines.Subscribe(total => TotalLines = total);
            var filterCount = tailer.MatchedLines.Subscribe(filtered => FilteredLines = filtered.Length);

            var loader = tailer.Lines.Connect()
                .Buffer(TimeSpan.FromMilliseconds(125))
                .FlattenBufferResult()
                .Transform(line => new LineProxy(line, (DateTime?)null))
                .Sort(SortExpressionComparer<LineProxy>.Ascending(proxy => proxy.Number))
                .ObserveOn(schedulerProvider.MainThread)
                .Bind(out _data)
                .Subscribe(a => logger.Info(a.Adds.ToString()), ex => logger.Error(ex, "Opps"));

            _cleanup = new CompositeDisposable(tailer, totalCount, loader, filterCount);
        }

        public string File { get; }

        public ReadOnlyObservableCollection<LineProxy> Lines => _data;

        public string SearchText
        {
            get => _searchText;
            set => SetAndRaise(ref _searchText, value);
        }

        public int TotalLines
        {
            get => _totalLines;
            set => SetAndRaise(ref _totalLines, value);
        }

        public int FilteredLines
        {
            get => _filteredLines;
            set => SetAndRaise(ref _filteredLines, value);
        }

        public void Dispose()
        {
            _cleanup.Dispose();
        }
    }
}
