namespace FileDissector.Domain.Infrastructure
{
    /// <summary>
    /// This interface makes it possible to pass around a DI container without being forced to rely on any specific library or implementation for it.
    /// So we create a derived class for this interface and pass the DI Container to it and all the <see cref="Get{T}"/> class will be delegated to the container.
    /// </summary>
    public interface IObjectProvider
    {
        T Get<T>();
    }
}
