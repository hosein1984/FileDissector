using System;
using System.IO;
using System.Linq;
using System.Threading;
using FileDissector.Domain.FileHandling;
using FluentAssertions;
using Xunit;

namespace FileDissector.Fixtures
{
    public class FileScanFixture
    {
        /*
            Putting a thread sleep into a test sucks. However the file system watcher which ScanLineNumbers()
            is based on is async by nature and since it is build from old fashioned events there is no way
            to pass it a scheduler.
         */

        [Fact]
        public void CanStreamFile()
        {
            var file = Path.GetTempFileName();
            var info = new FileInfo(file);
            int[] result = new int[0];

            File.AppendAllLines(file, Enumerable.Range(1,100).Select(i => $"{i}").ToArray());

            using (info.ScanLineNumbers().Subscribe(x => result = x))
            {
                result.ShouldAllBeEquivalentTo(Enumerable.Range(1,100));

                File.AppendAllLines(file, Enumerable.Range(101, 10).Select(i => $"{i}").ToArray());

                Thread.Sleep(TimeSpan.FromMilliseconds(100));

                File.Delete(file);

                result.ShouldAllBeEquivalentTo(Enumerable.Range(1,110));
            }
        }

        [Fact]
        public void CanStreamFileWithPredicate()
        {
            var file = Path.GetTempFileName();
            var info = new FileInfo(file);
            int[] result = new int[0];

            File.AppendAllLines(file, Enumerable.Range(1,100).Select(i => $"{i}").ToArray());

            using (info.ScanLineNumbers(i => int.Parse(i) % 2 == 1).Subscribe(x => result = x))
            {
                result.ShouldAllBeEquivalentTo(Enumerable.Range(1,100).Where(i => i %2 == 1));

                File.AppendAllLines(file, Enumerable.Range(101,10).Select(i => $"{i}").ToArray());

                Thread.Sleep(TimeSpan.FromMilliseconds(100));

                File.Delete(file);

                result.ShouldAllBeEquivalentTo(Enumerable.Range(1,110).Where(i => i % 2 == 1));
            }
        }
    }
}
