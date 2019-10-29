using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileDissector.Domain.FileHandling
{
    public class ScrollRequest
    {
        public int NumberOfItems { get; }
        public int FirstIndex { get; }
        public ScrollingType Type { get; }

        public ScrollRequest(int numberOfItems)
        {
            NumberOfItems = numberOfItems;
            Type = ScrollingType.Tail;
        }

        public ScrollRequest(int numberOfItems, int firstIndex)
        {
            NumberOfItems = numberOfItems;
            Type = ScrollingType.User;
            FirstIndex = firstIndex;
        }
    }
}
