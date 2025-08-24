using System.Collections.Specialized;
using static System.ArgumentNullException;

namespace MicroM.Core
{
    // .NET 9 includes a new OrderedDictionary implementation
    public class CustomOrderedDictionary<T> : IReadonlyOrderedDictionary<T>
    {
        private OrderedDictionary _Dictionary = new(StringComparer.OrdinalIgnoreCase);

        public bool Contains(string key) { return _Dictionary.Contains(key); }
        public int Count => _Dictionary.Count;

        public T? this[string key] => (T?)_Dictionary[key];

        public T? this[int index] => (T?)_Dictionary[index];

        public IEnumerable<T> Values => _Dictionary.Values.OfType<T>();
        public IEnumerable<string> Keys => _Dictionary.Keys.OfType<string>();

        public IEnumerator<T> GetEnumerator()
        {
            return _Dictionary.Values.Cast<T>().GetEnumerator();
        }

        public CustomOrderedDictionary<T> Add(string key, T value)
        {
            ThrowIfNull(value);
            _Dictionary.Add(key, value);
            return this;
        }

        public CustomOrderedDictionary<T> Remove(string key)
        {
            _Dictionary.Remove(key);
            return this;

        }

        public CustomOrderedDictionary<T> RemoveAt(int index)
        {
            if (index < 0 || index >= _Dictionary.Count) throw new ArgumentOutOfRangeException(nameof(index));
            _Dictionary.RemoveAt(index);
            return this;
        }

        public bool TryGetValue(string key, out T? value)
        {
            value = (T?)_Dictionary[key];
            if (value != null) return true;
            value = default;
            return false;
        }

        public bool TryAdd(string key, T value)
        {
            if (Contains(key)) return false;
            Add(key, value);
            return true;
        }

        public void Clear()
        {
            this._Dictionary.Clear();
        }
    }
}
