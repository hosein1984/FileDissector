using System;
using FileDissector.Domain.FileHandling;

namespace FileDissector.Views
{
    public class LineProxy
    {
        private readonly Line _line;

        public LineProxy(Line line, DateTime? time = null)
        {
            _line = line;
        }

        public int Number => _line.Number;
        public string Text => _line.Text;
    }
}
