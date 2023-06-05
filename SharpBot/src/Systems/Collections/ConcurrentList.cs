using System.Diagnostics.Contracts;

namespace SharpBot.Systems.Collections;

public class ConcurrentList<T> {
    private readonly List<T> _list = new();
    private readonly SemaphoreSlim _semaphoreSlim;

    public ConcurrentList() {
        _list ??= new List<T>();
        _semaphoreSlim = new SemaphoreSlim(1);
    }

    public ConcurrentList(List<T> list) : this() {
        _list = list;
    }

    public ConcurrentList(int capacity) : this() {
        _list = new List<T> {
            Capacity = capacity,
        };
    }

    public int Count => _list.Count;
    public int Capacity => _list.Capacity;

    [Obsolete("This method is NOT thread safe! Please use TryElementAt(int, out T?) instead!")]
    public T this[int index] {
        get {
            try {
                _semaphoreSlim.Wait();
                return _list[index];
            }
            finally {
                _semaphoreSlim.Release();
            }
        }
        set {
            try {
                _semaphoreSlim.Wait();
                _list[index] = value;
            }
            finally {
                _semaphoreSlim.Release();
            }
        }
    }

    public bool TryGetRandomElement(bool removeAfterGet, out T element) {
        element = default;
        if (!_semaphoreSlim.Wait(TimeSpan.FromSeconds(5))) return false;
        
        try {
            var targetIndx = Random.Shared.Next(_list.Count);
            element = _list[targetIndx];
            if (removeAfterGet)_list.RemoveAt(targetIndx);
            return true;
        } finally {
            _semaphoreSlim.Release();
        }
    }
    
    public bool TryElementAt(int index, out T? element) {
        element = default;
        if (!_semaphoreSlim.Wait(TimeSpan.FromSeconds(5))) return false;
        
        try {
            element = _list[index];
            return true;
        } finally {
            _semaphoreSlim.Release();
        }
    }
    
    [Pure]
    public List<T> ToList() {
        return _list.ToList();
        // Just call the extension, not much.
    }

    public bool Contains(T item) {
        ArgumentNullException.ThrowIfNull(item, nameof(item));

        if (!_semaphoreSlim.Wait(TimeSpan.FromSeconds(5))) return false;
        try {
            return _list.Contains(item);
        }
        finally {
            _semaphoreSlim.Release();
        }
    }

    public async Task<bool> ContainsAsync(T item) {
        ArgumentNullException.ThrowIfNull(item, nameof(item));

        if (!await _semaphoreSlim.WaitAsync(TimeSpan.FromSeconds(5))) return false;
        try {
            return _list.Contains(item);
        }
        finally {
            _semaphoreSlim.Release();
        }
    }

    public bool TryRemove(T item) {
        ArgumentNullException.ThrowIfNull(item, nameof(item));

        if (!_semaphoreSlim.Wait(TimeSpan.FromSeconds(5))) return false;
        _list.Remove(item);
        _semaphoreSlim.Release();
        return true;
    }

    public async Task<bool> TryRemoveAsync(T item) {
        ArgumentNullException.ThrowIfNull(item, nameof(item));

        if (!await _semaphoreSlim.WaitAsync(TimeSpan.FromSeconds(5))) return false;
        _list.Remove(item);
        _semaphoreSlim.Release();
        return true;
    }


    public bool TryRemoveAt(int index) {
        if (!_semaphoreSlim.Wait(TimeSpan.FromSeconds(5))) return false;
        if (index >= _list.Count) return false;
        _list.RemoveAt(index);
        _semaphoreSlim.Release();
        return true;
    }

    public async Task<bool> TryRemoveAtAsync(int index) {
        if (!await _semaphoreSlim.WaitAsync(TimeSpan.FromSeconds(5))) return false;
        if (index >= _list.Count) return false;
        _list.RemoveAt(index);
        _semaphoreSlim.Release();
        return true;
    }

    public bool TryAdd(T item) {
        ArgumentNullException.ThrowIfNull(item, nameof(item));

        if (!_semaphoreSlim.Wait(TimeSpan.FromSeconds(5))) return false;
        _list.Add(item);
        _semaphoreSlim.Release();
        return true;
    }

    public async Task<bool> TryAddAsync(T item) {
        ArgumentNullException.ThrowIfNull(item, nameof(item));

        if (!await _semaphoreSlim.WaitAsync(TimeSpan.FromSeconds(5))) return false;
        _list.Add(item);
        _semaphoreSlim.Release();
        return true;
    }

    public bool TryAddRange(IEnumerable<T> items) {
        ArgumentNullException.ThrowIfNull(items, nameof(items));

        if (!_semaphoreSlim.Wait(TimeSpan.FromSeconds(5))) return false;
        _list.AddRange(items);
        _semaphoreSlim.Release();
        return true;
    }

    public async Task<bool> TryAddRangeAsync(IEnumerable<T> items) {
        ArgumentNullException.ThrowIfNull(items, nameof(items));

        if (!await _semaphoreSlim.WaitAsync(TimeSpan.FromSeconds(5))) return false;
        _list.AddRange(items);
        _semaphoreSlim.Release();
        return true;
    }
}