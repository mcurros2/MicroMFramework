
namespace MicroM.Core
{
    public interface IReadonlyOrderedDictionary<T>
    {
        bool Contains(string key);

        int Count { get; }

        public T? this[string key] { get; }

        public T? this[int index] { get; }

        public IEnumerator<T> GetEnumerator();

        public IEnumerable<T> Values { get; }

        public IEnumerable<string> Keys { get; }

        public bool TryGetValue(string key, out T? value);

    }
}
