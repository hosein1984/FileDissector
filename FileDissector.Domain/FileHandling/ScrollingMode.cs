namespace FileDissector.Domain.FileHandling
{
    public enum ScrollingMode
    {
        /// <summary>
        /// Auto scroll to the tail
        /// </summary>
        Tail,

        /// <summary>
        /// The consumer specifies which starting index and page size
        /// </summary>
        User
    }
}
