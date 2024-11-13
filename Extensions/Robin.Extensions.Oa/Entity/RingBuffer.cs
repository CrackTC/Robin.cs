namespace Robin.Extensions.Oa.Entity;

internal class RingBuffer<T>(int size) where T : struct
{
    private readonly T[] _buffer = new T[size + 1];
    private int _head = 0;
    private int _tail = 0;

    public void Add(T item)
    {
        _buffer[_tail] = item;
        _tail = (_tail + 1) % (size + 1);
        if (_tail == _head)
            _head = (_head + 1) % (size + 1);
    }

    public IEnumerable<T> GetItems()
    {
        for (var i = _head; i != _tail; i = (i + 1) % (size + 1))
            yield return _buffer[i];
    }

    public T? Last => _head == _tail ? null : _buffer[(_tail + size) % (size + 1)];
}
