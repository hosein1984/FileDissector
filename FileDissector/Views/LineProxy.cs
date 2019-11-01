using System;
using FileDissector.Domain.FileHandling;

namespace FileDissector.Views
{
    /// <summary>
    /// This class is a proxy for <see cref="Line"/> class. It exposes the number and text property and adds the <see cref="IsRecent"/> property/
    /// </summary>
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
