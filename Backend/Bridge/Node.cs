namespace Calculator;

internal class Node<T>
{
    private Node<T>? _previous;
    private Node<T>? _next;

    public Node(T value)
    {
        Value = value;
        _previous = null;
        _next = null;
    }

    public T Value { get; }

    public void PointAt(Node<T>? node, bool setPreviousNode = true)
    {
        _next = node;
        if (node != null && setPreviousNode) node._previous = this;
    }

    public void PreviousWas(Node<T> node)
    {
        _previous = node;
    }

    public Node<T>? Previous()
    {
        return _previous;
    }

    public Node<T>? Next()
    {
        return _next;
    }

    public Node<T> Copy()
    {
        return new Node<T>(Value)
        {
            _previous = _previous,
            _next = _next
        };
    }
}