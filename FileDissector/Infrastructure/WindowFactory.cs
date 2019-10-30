using System;
using Dragablz;
using FileDissector.Domain.Infrastructure;

namespace FileDissector.Infrastructure
{
    public class WindowFactory : IWindowFactory
    {
        private readonly IObjectProvider _objectProvider;

        public WindowFactory(IObjectProvider objectProvider)
        {
            _objectProvider = objectProvider;
        }
        public MainWindow Create(bool showMenu = false)
        {
            var window = new MainWindow();
            var model = _objectProvider.Get<WindowViewModel>();

            window.DataContext = model;

            window.Closing += (sender, args) =>
            {
                if (TabablzControl.GetIsClosingAsPartOfDragOperation(window)) return;

                var toDispose = ((MainWindow) sender).DataContext as IDisposable;
                toDispose?.Dispose();
            };

            return window;
        }
    }
}
