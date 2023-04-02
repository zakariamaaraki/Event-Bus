using System.Collections.Concurrent;

namespace Service_bus.Models.LinkedDictionary;

/// <summary>
/// This class represents a Dictionary which contains elements in order of insertion.
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TValue"></typeparam>
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

    /// <summary>
    /// Adds / Updates element to the dictionary
    /// </summary>
    /// <param name="key">The key</param>
    /// <param name="value">The value</param>
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

    /// <summary>
    /// Retrieves an element from the dictionary based on it's key.
    /// </summary>
    /// <param name="key">The key</param>
    /// <returns>Return the value or throw KeyNotFoundException incase the key was not found.</returns>
    public TValue? Get(TKey key)
    {
        if (_dictionary.TryGetValue(key, out Node<TKey, TValue>? node))
        {
            return node.Value;
        }
        throw new KeyNotFoundException($"key {key} does not exist");
    }

    /// <summary>
    /// Checks if a key exists in the dictionary.
    /// </summary>
    /// <param name="key">The key</param>
    /// <returns>True if the key exists, False if it's absent.</returns>
    public bool ContainsKey(TKey key)
    {
        return _dictionary.ContainsKey(key);
    }

    /// <summary>
    /// Remove an element from the dictionary based on it's key.
    /// </summary>
    /// <param name="key">The key</param>
    /// <returns>Return True if the element was found and deleted, otherwise returns False</returns>
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

    /// <summary>
    /// Removes an element from the dictionary based on a condition/
    /// </summary>
    /// <param name="function">A predicat function returning Whether the element should be deleted or not.</param>
    /// <returns>Returns a list of deleted elements.</returns>
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

    /// <summary>
    /// Computes the number of elements in the dictionary.
    /// </summary>
    /// <returns>Returns the number of elements in the dictionary.</returns>
    public int Count()
    {
        return _dictionary.Count;
    }
}