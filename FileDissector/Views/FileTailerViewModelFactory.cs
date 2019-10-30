using System;
using System.IO;
using FileDissector.Domain.Infrastructure;

namespace FileDissector.Views
{
    public class FileTailerViewModelFactory
    {
        private readonly IObjectProvider _objectProvider;

        public FileTailerViewModelFactory(IObjectProvider objectProvider)
        {
            _objectProvider = objectProvider;
        }

        public FileTailerViewModel Create(FileInfo fileInfo)
        {
            if (fileInfo == null) throw new ArgumentNullException(nameof(fileInfo));

            return new FileTailerViewModel(_objectProvider.Get<ILogger>(), _objectProvider.Get<ISchedulerProvider>(), fileInfo);
        }
    }
}
