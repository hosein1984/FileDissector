using FileDissector.Domain.Infrastructure;
using StructureMap;

namespace FileDissector.Infrastructure
{
    public class ObjectProvider : IObjectProvider
    {
        private readonly IContainer _container;

        public ObjectProvider(IContainer container)
        {
            _container = container;
        }

        public T Get<T>()
        {
            return _container.GetInstance<T>();
        }
    }
}
