using System;
using System.Collections.Specialized;
using System.Linq;
using static System.ArgumentNullException;

namespace MicroM.Core
{
    /// <summary>
    /// Provides an ordered dictionary implementation with case-insensitive keys.
    /// </summary>
    public class CustomOrderedDictionary<T> : IReadonlyOrderedDictionary<T>
    {
        private OrderedDictionary _Dictionary = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Determines whether the dictionary contains the specified key.
        /// </summary>
        public bool Contains(string key) { return _Dictionary.Contains(key); }

        /// <summary>
        /// Gets the number of elements in the dictionary.
        /// </summary>
        public int Count => _Dictionary.Count;

        /// <summary>
        /// Gets the element with the specified key.
        /// </summary>
        public T? this[string key] => (T?)_Dictionary[key];

        /// <summary>
        /// Gets the element at the specified index.
        /// </summary>
        public T? this[int index] => (T?)_Dictionary[index];

        /// <summary>
        /// Gets the collection of values.
        /// </summary>
        public IEnumerable<T> Values => _Dictionary.Values.OfType<T>();

        /// <summary>
        /// Gets the collection of keys.
        /// </summary>
        public IEnumerable<string> Keys => _Dictionary.Keys.OfType<string>();

        /// <summary>
        /// Returns an enumerator that iterates through the dictionary values.
        /// </summary>
        public IEnumerator<T> GetEnumerator()
        {
            return _Dictionary.Values.Cast<T>().GetEnumerator();
        }

        /// <summary>
        /// Adds an element with the provided key and value.
        /// </summary>
        public CustomOrderedDictionary<T> Add(string key, T value)
        {
            ThrowIfNull(value);
            _Dictionary.Add(key, value);
            return this;
        }

        /// <summary>
        /// Removes the element with the specified key.
        /// </summary>
        public CustomOrderedDictionary<T> Remove(string key)
        {
            _Dictionary.Remove(key);
            return this;
        }

        /// <summary>
        /// Removes the element at the specified index.
        /// </summary>
        public CustomOrderedDictionary<T> RemoveAt(int index)
        {
            if (index < 0 || index >= _Dictionary.Count) throw new ArgumentOutOfRangeException(nameof(index));
            _Dictionary.RemoveAt(index);
            return this;
        }

        /// <summary>
        /// Attempts to retrieve a value for the specified key.
        /// </summary>
        public bool TryGetValue(string key, out T? value)
        {
            value = (T?)_Dictionary[key];
            if (value != null) return true;
            value = default;
            return false;
        }

        /// <summary>
        /// Adds the specified key and value if the key does not already exist.
        /// </summary>
        public bool TryAdd(string key, T value)
        {
            if (Contains(key)) return false;
            Add(key, value);
            return true;
        }

        /// <summary>
        /// Removes all elements from the dictionary.
        /// </summary>
        public void Clear()
        {
            _Dictionary.Clear();
        }
    }
}
