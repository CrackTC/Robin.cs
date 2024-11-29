namespace Robin.Abstractions.Event;

public class EventFilter(IEnumerable<long> ids, bool whitelist = false)
{
    private readonly HashSet<long> _ids = ids.ToHashSet();
    public IEnumerable<long> Ids => _ids;

    public bool Whitelist { get; } = whitelist;

    public bool IsIdEnabled(long id) => Whitelist ? _ids.Contains(id) : !_ids.Contains(id);

    public void EnableOn(long id)
    {
        if (Whitelist)
            _ids.Add(id);
        else
            _ids.Remove(id);
    }

    public void DisableOn(long id)
    {
        if (Whitelist)
            _ids.Remove(id);
        else
            _ids.Add(id);
    }
}
