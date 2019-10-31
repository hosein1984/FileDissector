using System;
using FileDissector.Domain.FileHandling;

namespace FileDissector.Views
{
    public class LineProxy
    {
        private readonly Line _line;

        public LineProxy(Line line)
        {
            _line = line;
            IsRecent = line.Timestamp.HasValue && DateTime.Now.Subtract(line.Timestamp.Value).TotalSeconds < 2;
        }

        public bool IsRecent { get; }

        public int Number => _line.Number;
        public string Text => _line.Text;
    }
}
