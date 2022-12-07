using Bridge;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Calculator
{
    internal static class PointUtils
    {
        public static double GetTotalDistance(IReadOnlyList<Point> pointsToOrder, IEnumerable<int> indexes)
        {
            var points = indexes.Select(i => pointsToOrder[i]).ToList();
            return points.GetTotalDistance();
        }

        public static double GetTotalDistance(this IEnumerable<Point> points)
        {
            double result = 0;
            var previousPoint = points.First();
            foreach (var point in points.Skip(1))
            {
                result += previousPoint.Distance(point);
                previousPoint = point;
            }

            return result;
        }

        public static double GetTotalDistance(this IEnumerable<Node<Point>> points)
        {
            return points.Sum(node => node.Value.Distance(node.Next()!.Value));
        }
    }
}
