using System;

namespace FileDissector.Infrastructure
{
    public class ViewContainer
    {
        public ViewContainer(string title, object content)
        {
            Title = title;
            Content = content;
            Id = Guid.NewGuid();
        }

        public Guid Id { get; }
        public object Content { get; }
        public string Title { get; }
    }
}
