namespace Service_bus.Models.LinkedDictionary;

public class Node<TKey, TValue>  where TKey : notnull
{
    public TKey? Key { get; set;  }
    public TValue? Value { get; set; }
    public Node<TKey, TValue>? Next { get; set; }
    public Node<TKey, TValue>? Prev { get; set; }
}