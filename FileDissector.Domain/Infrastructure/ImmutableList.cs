using System;

namespace FileDissector.Domain.Infrastructure
{
    public class ImmutableList<T>
    {
        private static readonly ImmutableList<T> Empty = new ImmutableList<T>();

        private readonly T[] _data;

        public ImmutableList()
        {
            _data = new T[0];
        }

        public ImmutableList(T[] data)
        {
            _data = data;
        }

        public T[] Data => _data;

        public ImmutableList<T> Add(T value)
        {
            var newData = new T[_data.Length + 1];

            Array.Copy(_data, newData, _data.Length);
            newData[_data.Length] = value;

            return new ImmutableList<T>(newData);
        }

        public ImmutableList<T> Add(T[] values)
        {
            var result = new T[_data.Length + values.Length];
            _data.CopyTo(result,0);
            values.CopyTo(result, _data.Length);

            return new ImmutableList<T>(result);
        }

        public ImmutableList<T> Remove(T value)
        {
            var i = IndexOf(value);
            if (i < 0)
            {
                return this;
            }

            var length = _data.Length;
            if (length == 1)
            {
                return Empty;
            }

            var newData = new T[length - 1];
            Array.Copy(_data, 0, newData, 0, i);
            Array.Copy(_data, i + 1, newData, i, length - i - 1);

            return new ImmutableList<T>(newData);
        }

        private int IndexOf(T value)
        {
            return Array.IndexOf(_data, value);
        }
    }
}
