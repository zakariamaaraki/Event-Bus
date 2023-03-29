using System.Collections.Concurrent;

namespace Service_bus.Models.LinkedDictionary;

public class LinkedDictionary<TKey, TValue> where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, Node<TKey, TValue>> _dictionary;
    private Node<TKey, TValue>? _last;
    private Node<TKey, TValue>? _first;

    // TODO: use semaphore

    public LinkedDictionary()
    {
        _dictionary = new ConcurrentDictionary<TKey, Node<TKey, TValue>>();
    }

    public void Put(TKey key, TValue value)
    {
        var newNode = new Node<TKey, TValue>
        {
            Key = key,
            Value = value,
            Next = null,
            Prev = _last
        };

        if (_last != null)
        {
            _last.Next = newNode;
        }
        else
        {
            _first = newNode;
        }

        _last = newNode;

        _dictionary[key] = newNode;
    }

    public TValue? Get(TKey key)
    {
        if (_dictionary.TryGetValue(key, out Node<TKey, TValue>? node))
        {
            return node.Value;
        }
        throw new KeyNotFoundException($"key {key} does not exist");
    }

    public bool ContainsKey(TKey key)
    {
        return _dictionary.ContainsKey(key);
    }

    public bool Remove(TKey key)
    {
        if (_dictionary.TryGetValue(key, out Node<TKey, TValue>? node))
        {
            if (_first == node)
            {
                _first = node.Next;
            }

            if (node.Next != null)
            {
                node.Next.Prev = node.Prev;
            }

            if (node.Prev != null)
            {
                node.Prev.Next = node.Next;
            }

            _dictionary.Remove(key, out Node<TKey, TValue>? _);
            return true;
        }

        return false;
    }

    public List<(TKey, TValue)> RemoveBasedOnCondition(Func<TValue, bool> function)
    {
        Node<TKey, TValue>? iter = _first;
        List<(TKey, TValue)> list = new();

        while (iter != null)
        {
            if (iter.Value != null && function(iter.Value))
            {
                list.Add((iter.Key, iter.Value));
                Remove(iter.Key);
            }
            else
            {
                // The list is sorted
                break;
            }

            iter = iter.Next;
        }

        return list;
    }

    public int Count()
    {
        return _dictionary.Count;
    }
}