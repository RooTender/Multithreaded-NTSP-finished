using Bridge;

namespace Calculator
{
    public class BestCycle
    {
        private List<Node<Point>> _bestResult;
        
        public BestCycle(List<Point> bestResult)
        {
            _bestResult = PointsToNodeCycle(bestResult);
        }

        public List<Point> GetBest()
        {
            return NodeCycleToPoints(_bestResult);
        }
        
        public IEnumerable<Point> Find()
        {
            if (_bestResult.Count < 6) return GetBest();
            
            for (var i = 0; i < _bestResult.Count - 1; ++i)
            {
                var blue  = (i, i + 1);
                for (var j = i + 2; j < _bestResult.Count - 1; ++j)
                {
                    var green = (j, j + 1);
                    for (var k = j + 2; k < _bestResult.Count - 1; ++k)
                    {
                        var red = (k, k + 1);

                        var variationA = GetVariationA(blue, green, red, _bestResult);
                        var variationB = GetVariationB(blue, green, red, _bestResult);

                        var currentlyBestResult = _bestResult.GetTotalDistance();
                        if (currentlyBestResult > variationA.GetTotalDistance())
                        {
                            _bestResult = variationA;
                            currentlyBestResult = _bestResult.GetTotalDistance();
                        }

                        if (currentlyBestResult > variationB.GetTotalDistance())
                        {
                            _bestResult = variationB;
                        }

                        Globals.Counter++;
                    }
                }
            }

            return GetBest();
        }

        private static List<Point> NodeCycleToPoints(IReadOnlyList<Node<Point>> cycle)
        {
            var result = new List<Point>();

            var node = cycle[0];
            for (var i = 0; i < cycle.Count; ++i)
            {
                result.Add(node!.Value);
                node = node.Next();
            }

            return result;
        }

        private static List<Node<Point>> PointsToNodeCycle(IReadOnlyList<Point> source)
        {
            var cycle = new List<Node<Point>> { new(source[0]) };
            for (var i = 1; i < source.Count; i++)
            {
                cycle.Add(new Node<Point>(source[i]));
                cycle[i - 1].PointAt(cycle[i]);
            }
            cycle[source.Count - 1].PointAt(cycle[0]);

            return cycle;
        }

        private static List<Node<Point>> GetVariationA((int, int) blue, (int, int) green, (int, int) red, IReadOnlyList<Node<Point>> source)
        {
            var result = DeepCopyCycle(source);

            NullifyEdgesBetweenColors(blue, green, red, ref result);
            result[blue.Item1 ].PointAt(result[green.Item1], false);
            result[blue.Item2 ].PointAt(result[red.Item1  ], false);
            result[green.Item2].PointAt(result[red.Item2  ], false);

            return FixCycleVariation(result);
        }

        private static List<Node<Point>> GetVariationB((int, int) blue, (int, int) green, (int, int) red, IReadOnlyList<Node<Point>> source)
        {
            var result = DeepCopyCycle(source);

            NullifyEdgesBetweenColors(blue, green, red, ref result);
            result[blue.Item1].PointAt(result[green.Item2], false);
            result[red.Item1 ].PointAt(result[green.Item1], false);
            result[blue.Item2].PointAt(result[red.Item2  ], false);

            return FixCycleVariation(result);
        }

        private static List<Node<Point>> DeepCopyCycle(IReadOnlyList<Node<Point>> source)
        {
            var result = new List<Node<Point>>
            {
                new(source[0].Value)
            };
            for (var i = 1; i < source.Count; i++)
            {
                var point = new Node<Point>(source[i].Value);
                result[i - 1].PointAt(point);

                result.Add(point);
            }
            result[^1].PointAt(result[0]);

            return result;
        }

        private static void NullifyEdgesBetweenColors((int, int) blue, (int, int) green, (int, int) red, ref List<Node<Point>> source)
        {
            source[blue.Item1 ].PointAt(null, false);
            source[green.Item1].PointAt(null, false);
            source[red.Item1  ].PointAt(null, false);
        }

        private static List<Node<Point>> FixCycleVariation(List<Node<Point>> cycle)
        {
            var previousNode = cycle[0];
            var node = previousNode.Next();

            for (var i = 0; i < cycle.Count - 1; ++i)
            {
                if (node!.Next() == null)
                {
                    node.PointAt(node.Previous(), false);
                }
                else if (node.Next()?.Value == previousNode.Value)
                {
                    node.PointAt(node.Previous(), false);
                    node.PreviousWas(previousNode);
                }

                node.PreviousWas(previousNode);
                previousNode = node;
                node = node.Next();
            }

            return cycle;
        }
    }
}
