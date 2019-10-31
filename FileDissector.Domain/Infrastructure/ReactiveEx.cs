using System;
using System.Reactive;
using System.Reactive.Linq;

namespace FileDissector.Domain.Infrastructure
{
    public static class ReactiveEx
    {

        public static IObservable<TSource> Previous<TSource>(this IObservable<TSource> source)
        {
            return source.PairWithPrevious().Select(pair => pair.Previous);
        }

        public static IObservable<CurrentAndPrevious<TSource>> PairWithPrevious<TSource>(
            this IObservable<TSource> source)
        {
            return source.Scan(Tuple.Create(default(TSource), default(TSource)),
                    (acc, cur) => Tuple.Create(acc.Item2, cur))
                .Select(pair => new CurrentAndPrevious<TSource>(pair.Item1, pair.Item2));
        }

        public class CurrentAndPrevious<T>
        {
            public CurrentAndPrevious(T current, T previous)
            {
                Current = current;
                Previous = previous;
            }

            public T Current { get; }
            public T Previous { get; }
        }

        public static IObservable<Unit> ToUnit<T>(this IObservable<T> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.Select(_ => Unit.Default);
        }

        public static IObservable<Unit> StartWithUnit(this IObservable<Unit> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.StartWith(Unit.Default);
        }
    }
}
