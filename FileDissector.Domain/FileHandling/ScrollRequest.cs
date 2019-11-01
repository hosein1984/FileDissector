namespace FileDissector.Domain.FileHandling
{
    /// <summary>
    /// Represents a ScrollRequest that can be send to the <see cref="FileTailer"/> to load a specific part of the file 
    /// </summary>
    public class ScrollRequest
    {
        public int PageSize { get; }
        public int FirstIndex { get; }
        public ScrollingMode Mode { get; }

        public ScrollRequest(int pageSize)
        {
            PageSize = pageSize;
            Mode = ScrollingMode.Tail;
        }

        public ScrollRequest(int pageSize, int firstIndex)
        {
            PageSize = pageSize;
            FirstIndex = firstIndex;
            Mode = ScrollingMode.User;
        }
    }
}
