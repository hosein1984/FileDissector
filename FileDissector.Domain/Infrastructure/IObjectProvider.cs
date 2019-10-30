namespace FileDissector.Domain.Infrastructure
{
    public interface IObjectProvider
    {
        T Get<T>();
    }
}
