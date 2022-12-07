using System;

namespace Bridge;

public class Point
{
    public double X { get; set; }
    public double Y { get; set; }

    public Point(double x, double y)
    {
        X = x;
        Y = y;
    }

    public double Distance(Point destination)
    {
        return Math.Sqrt(
            (destination.X - X) * (destination.X - X) +
            (destination.Y - Y) * (destination.Y - Y));
    }
}
