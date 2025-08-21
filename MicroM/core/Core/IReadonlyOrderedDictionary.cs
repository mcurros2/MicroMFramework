using System.Collections.Generic;

namespace MicroM.Core
{
    /// <summary>
    /// Represents a read-only ordered dictionary.
    /// </summary>
    public interface IReadonlyOrderedDictionary<T>
    {
        /// <summary>
        /// Determines whether the dictionary contains the specified key.
        /// </summary>
        bool Contains(string key);

        /// <summary>
        /// Gets the number of elements contained in the dictionary.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets the element associated with the specified key.
        /// </summary>
        T? this[string key] { get; }

        /// <summary>
        /// Gets the element at the specified index.
        /// </summary>
        T? this[int index] { get; }

        /// <summary>
        /// Returns an enumerator that iterates through the values.
        /// </summary>
        IEnumerator<T> GetEnumerator();

        /// <summary>
        /// Gets the collection of values.
        /// </summary>
        IEnumerable<T> Values { get; }

        /// <summary>
        /// Gets the collection of keys.
        /// </summary>
        IEnumerable<string> Keys { get; }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        bool TryGetValue(string key, out T? value);
    }
}
