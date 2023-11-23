using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using MCollections;

namespace EmailClient.Gui.Collection;

public class ObservableIndexedSet<T> : IndexedSet<T>, INotifyCollectionChanged
{
    public event NotifyCollectionChangedEventHandler? CollectionChanged;
    public ObservableIndexedSet() : base()
    {
    }
    public ObservableIndexedSet(IComparer<T> comparer) : base(comparer)
    {
    }

    public new bool Add(T item)
    {
        var success = base.Add(item);
        if (!success) return false;
        CollectionChanged?.Invoke(this, new(NotifyCollectionChangedAction.Add, item, IndexOfKey(item)));
        return true;
    }
    public new bool Remove(T item)
    {
        var index = IndexOfKey(item);
        if (index == -1) return false;
        base.Remove(item);
        CollectionChanged?.Invoke(this, new(NotifyCollectionChangedAction.Remove, item, index));
        return true;
    }
    public new void Clear()
    {
        base.Clear();
        CollectionChanged?.Invoke(this, new(NotifyCollectionChangedAction.Reset, Array.Empty<T>()));
    }
}