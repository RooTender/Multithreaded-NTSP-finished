using Bridge;

namespace GUI.Model;

public class Node
{
    public int? Id { get; set; }
    public double? X { get; set; }
    public double? Y { get; set; }

    public Node(int? id, double? x, double? y)
    {
        Id = id;
        X = x;
        Y = y;
    }

    public Node(int? id, Point dataPoint)
    {
        Id = id;
        X = dataPoint.X;
        Y = dataPoint.Y;
    }

    public Node(Node node)
    {
        Id = node.Id;
        X = node.X;
        Y = node.Y;
    }
}
