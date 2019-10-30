using System.Windows;
using System.Windows.Controls;

namespace FileDissector.Infrastructure
{
    public class AutoScroller : IDependencyObjectReceiver
    {
        private ScrollViewer _scrollViewer;

        public void Receive(DependencyObject value)
        {
            _scrollViewer = (ScrollViewer) value;
        }

        public void ScrollToEnd()
        {
            _scrollViewer?.ScrollToEnd();
        }
    }
}
