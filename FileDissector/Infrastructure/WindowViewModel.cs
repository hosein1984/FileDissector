using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Windows.Input;
using Dragablz;
using DynamicData.Binding;
using FileDissector.Domain.Infrastructure;
using FileDissector.Views;
using Microsoft.Win32;

namespace FileDissector.Infrastructure
{
    public class WindowViewModel : AbstractNotifyPropertyChanged, IDisposable
    {
        private readonly IObjectProvider _objectProvider;
        private readonly IDisposable _cleanup;
        private ViewContainer _selected;

        public ObservableCollection<ViewContainer> Views { get; } = new ObservableCollection<ViewContainer>();
        public IInterTabClient InterTabClient { get; }
        public ICommand OpenFileCommand { get; }
        public ICommand ShowInGithubCommand { get; }

        public WindowViewModel(IObjectProvider objectProvider, IWindowFactory windowFactory)
        {
            _objectProvider = objectProvider;
            InterTabClient = new InterTabClient(windowFactory);

            OpenFileCommand = new Command(OpenFile);
            ShowInGithubCommand = new Command(() => Process.Start("https://github.com/hosein1984"));

            _cleanup = Disposable.Create(() =>
            {
                Views.Select(vc => vc.Content).OfType<IDisposable>().ForEach(disposable => disposable.Dispose());
            });
        }

        public ItemActionCallback ClosingTabItemHandler => ClosingTabItemHandlerImpl;

        private void ClosingTabItemHandlerImpl(ItemActionCallbackArgs<TabablzControl> args)
        {
            var container = (ViewContainer) args.DragablzItem.DataContext;
            if (container.Equals(Selected))
            {
                Selected = Views.FirstOrDefault(vc => vc != container);
            }

            var disposable = container.Content as IDisposable;
            disposable?.Dispose();
        }

        public ViewContainer Selected
        {
            get => _selected;
            set => SetAndRaise(ref _selected, value);
        }

        private void OpenFile()
        {
            var dialog = new OpenFileDialog {Filter = "All files (*.*)|*.*"};
            var result = dialog.ShowDialog();

            if (result != true) return;

            var file = new FileInfo(dialog.FileName);

            var factory = _objectProvider.Get<FileTailerViewModelFactory>();
            var viewModel = factory.Create(file);

            var newItem = new ViewContainer(file.Name, viewModel);
            Views.Add(newItem);
            Selected = newItem;
        }

        public void Dispose()
        {
            _cleanup.Dispose();
        }
    }
}
