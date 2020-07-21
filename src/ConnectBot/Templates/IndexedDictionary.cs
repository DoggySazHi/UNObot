using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace ConnectBot.Templates
{
    public interface IReadOnlyIndexedDictionary<T, TU> : IReadOnlyDictionary<T, TU>
    {
        (T Key, TU Value) this[int i] { get; set; }
        (T Key, TU Value) AtIndex(int i);
        public void SetIndex(int i, T key, TU value);
    }
    
    public class IndexedDictionary<T, TU> : IReadOnlyIndexedDictionary<T, TU>
    {
        [JsonProperty] private Type _aType;
        [JsonProperty] private Type _bType;
        
        [JsonProperty]
        private List<T> _a;
        
        [JsonProperty]
        private List<TU> _b;
        
        public IndexedDictionary()
        {
            _aType = typeof(T);
            _bType = typeof(TU);
            _a = new List<T>();
            _b = new List<TU>();
        }

        public bool Add(T first, TU second)
        {
            if (_a.Contains(first))
                return false;
            _a.Add(first);
            _b.Add(second);
            return true;
        }

        public bool Add((T, TU) value) => Add(value.Item1, value.Item2);

        public bool TryGetValue(T item, out TU value)
        {
            var index = this._a.FindIndex(o => EqualityComparer<T>.Default.Equals(o, item));
            if (index < 0)
            {
                value = default;
                return false;
            }
            value = _b[index];
            return true;
        }

        [JsonIgnore]
        TU IReadOnlyDictionary<T, TU>.this[T key] => this[key];

        [JsonIgnore]
        public IEnumerable<T> Keys => _a;
        
        [JsonIgnore]

        public IEnumerable<TU> Values => _b;

        public TU this[T item]
        {
            get
            {
                var index = this._a.FindIndex(o => EqualityComparer<T>.Default.Equals(o, item));
                return index < 0 ? throw new KeyNotFoundException() : _b[index];
            }
            set
            {
                var index = this._a.FindIndex(o => EqualityComparer<T>.Default.Equals(o, item));
                if (index < 0)
                    throw new KeyNotFoundException();
                _b[index] = value;
            }
        }
        
        public (T Key, TU Value) this[int i]
        {
            get => (_a[i], _b[i]);
            set
            {
                _a[i] = value.Item1;
                _b[i] = value.Item2;
            }
        }

        public (T Key, TU Value) AtIndex(int i) => this[i];

        public void SetIndex(int i, T key, TU value)
        {
            _a[i] = key;
            _b[i] = value;
        }

        public bool Remove(T value)
        {
            var index = _a.FindIndex(o => EqualityComparer<T>.Default.Equals(o, value));
            if (index < 0) return false;
            _a.RemoveAt(index);
            _b.RemoveAt(index);
            return true;
        }
        
        public bool ContainsKey(T value) => _a.Any(o => EqualityComparer<T>.Default.Equals(o, value));
        public IEnumerator<KeyValuePair<T, TU>> GetEnumerator()
        {
            return _a.Select((t, i) => new KeyValuePair<T, TU>(t, _b[i])).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        [JsonIgnore]
        public int Count => _a.Count;

        public void RemoveAt(int index)
        {
            _a.RemoveAt(index);
            _b.RemoveAt(index);
        }

        public void Clear()
        {
            _a.Clear();
            _b.Clear();
        }
    }
}