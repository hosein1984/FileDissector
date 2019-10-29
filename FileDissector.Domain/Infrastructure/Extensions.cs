﻿using System;

namespace System
{
    public static class Extensions
    {
        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            return source.IndexOf(toCheck, comp) >= 0;
        }

        public static bool NetBoolean(this Random source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.NextDouble() > 0.5;
        }

        public static string Pluralise(this string source, int count)
        {
            return count == 1 
                ? $"{count} {source}" 
                : $"{count} {source}s";
        }
    }
}

namespace System.Collections.Generic
{

    public static class Extensions
    {
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (action == null) throw new ArgumentNullException(nameof(action));

            foreach (var item in source)
            {
                action(item);
            }
        }

        public static void ForEach<T>(this IEnumerable<T> source, Action<T,int> action)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (action == null) throw new ArgumentNullException(nameof(action));

            int i = 0;
            foreach (var item in source)
            {
                action(item, i++);
            }
        }
    }

}
