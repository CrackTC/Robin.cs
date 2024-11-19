namespace Robin.Extensions.Oa.Entity;

internal class RingBuffer<T>(int size) where T : struct
{
    private readonly T[] _buffer = new T[size + 1];
    private int _head = 0;
    private int _tail = 0;

    private int Increase(int index) => (index + 1) % (size + 1);
    private int Decrease(int index) => (index + size) % (size + 1);

    public void Add(T item)
    {
        _buffer[_tail] = item;
        _tail = Increase(_tail);
        if (_tail == _head)
            _head = Increase(_head);
    }

    public IEnumerable<T> GetItems()
    {
        for (var i = Decrease(_tail); i != Decrease(_head); i = Decrease(i))
            yield return _buffer[i];
    }

    public T? Last => _head == _tail ? null : _buffer[Decrease(_tail)];
}
