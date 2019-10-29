namespace FileDissector.Domain.Infrastructure
{
    public interface IObjectFactory
    {
        T Get<T>();
    }
}
