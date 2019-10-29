using System.IO;
using System.Linq;
using FileDissector.Domain.FileHandling;
using FluentAssertions;
using Xunit;

namespace FileDissector.Fixtures
{
    public class ReadLinesFixure
    {
        [Fact]
        public void ReadSpecificLines()
        {
            var file = Path.GetTempFileName();
            var info = new FileInfo(file);

            File.AppendAllLines(file, Enumerable.Range(1,100).Select(i => $"{i}").ToArray());

            var lines = info.ReadLines(new[] {1, 2, 3, 10, 100, 105});

            lines.Select(l => l.Number).ShouldAllBeEquivalentTo(new[] { 1, 2, 3, 10, 100 });

            File.Delete(file);
        }
    }
}
