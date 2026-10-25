using System;
using System.Collections.Generic;
using System.Linq;

namespace NileCore.Raar.Core.Extensions
{
    /// <summary>
    /// Extension methods for collections and enumerables.
    /// </summary>
    public static class CollectionExtensions
    {
        /// <summary>
        /// Executes an action on each element of the enumerable sequence.
        /// </summary>
        public static void ForEach<T>(this IEnumerable<T>? source, Action<T> action)
        {
            if (source == null || action == null) return;
            foreach (var item in source)
            {
                action(item);
            }
        }

        /// <summary>
        /// Splits an enumerable collection into chunks/batches of the specified size.
        /// </summary>
        public static IEnumerable<List<T>> Batch<T>(this IEnumerable<T>? source, int batchSize)
        {
            if (source == null) yield break;
            if (batchSize <= 0) throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be greater than zero.");

            var currentBatch = new List<T>(batchSize);
            foreach (var item in source)
            {
                currentBatch.Add(item);
                if (currentBatch.Count == batchSize)
                {
                    yield return currentBatch;
                    currentBatch = new List<T>(batchSize);
                }
            }

            if (currentBatch.Count > 0)
            {
                yield return currentBatch;
            }
        }

        /// <summary>
        /// Returns distinct elements from a sequence by using a specified key selector.
        /// </summary>
        public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T>? source, Func<T, TKey> keySelector)
        {
            if (source == null) yield break;
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));

            var seenKeys = new HashSet<TKey>();
            foreach (var item in source)
            {
                if (seenKeys.Add(keySelector(item)))
                {
                    yield return item;
                }
            }
        }

        /// <summary>
        /// Determines whether the enumerable is null or contains no elements.
        /// </summary>
        public static bool IsNullOrEmpty<T>(this IEnumerable<T>? source)
        {
            if (source == null) return true;
            if (source is ICollection<T> collection) return collection.Count == 0;
            if (source is IReadOnlyCollection<T> readOnlyCollection) return readOnlyCollection.Count == 0;
            return !source.Any();
        }

        /// <summary>
        /// Returns a randomly shuffled copy of the collection.
        /// </summary>
        public static List<T> Shuffle<T>(this IEnumerable<T>? source)
        {
            if (source == null) return new List<T>();
            var list = source.ToList();
            var rng = new Random();
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
            return list;
        }
    }
}
