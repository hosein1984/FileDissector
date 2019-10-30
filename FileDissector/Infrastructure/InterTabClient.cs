using System.Windows;
using Dragablz;

namespace FileDissector.Infrastructure
{
    public class InterTabClient : IInterTabClient
    {
        private readonly IWindowFactory _windowFactory;

        public InterTabClient(IWindowFactory windowFactory)
        {
            _windowFactory = windowFactory;
        }

        public INewTabHost<Window> GetNewHost(IInterTabClient interTabClient, object partition, TabablzControl source)
        {
            var window = _windowFactory.Create();

            return new NewTabHost<Window>(window, window.InitialTabablzControl);
        }

        public TabEmptiedResponse TabEmptiedHandler(TabablzControl tabControl, Window window)
        {
            return TabEmptiedResponse.CloseWindowOrLayoutBranch;
        }
    }
}
