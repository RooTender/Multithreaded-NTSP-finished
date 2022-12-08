using Bridge;
using System.Diagnostics.Metrics;
using System.Reflection;

namespace Calculator
{
    public class PMX
    {
        private List<int> _firstParent;
        private List<int> _secondParent;

        private  List<Point> _points;
        
        private IReadOnlyList<int> _bestResult;
        private (IReadOnlyList<int>, IReadOnlyList<int>) _bestParents;

        public PMX(List<Point> points)
        {
            _points = points;
            _bestResult = Enumerable.Range(0, points.Count).ToList();

            _firstParent = GetPermutation(points.Count);
            _secondParent = GetPermutation(points.Count);

            _bestParents = (_firstParent, _secondParent);
        }
        public PMX(List<Point> firstParent, List<Point> secondParent)
        {
			_points = firstParent;
			_firstParent = firstParent.Select(i => _points.IndexOf(i)).ToList();
			_secondParent = secondParent.Select(i => _points.IndexOf(i)).ToList();

			_bestResult = Enumerable.Range(0, _points.Count).ToList();
			_bestParents = (_firstParent, _secondParent);
		}

		public List<List<Point>> BestGeneration()
        {
            return new List<List<Point>>()
            {
                _bestParents.Item1.Select(i => _points[i]).ToList(), _bestParents.Item2.Select(i => _points[i]).ToList()
            };
        }

		public void NextGeneration()
		{
			var pointsAmount = _points.Count;
			var sequenceData = CreateSubsequence(pointsAmount);

			var firstParent = NextGenerationChild(_firstParent, _secondParent, sequenceData).ToList();
			var secondParent = NextGenerationChild(_secondParent, _firstParent, sequenceData).ToList();

			var bestResultDistance = PointUtils.GetTotalDistance(_points, _bestResult);
			if (bestResultDistance > PointUtils.GetTotalDistance(_points, firstParent))
			{
				_bestResult = firstParent;
				_bestParents = (_firstParent, _secondParent);
			}

			if (bestResultDistance > PointUtils.GetTotalDistance(_points, secondParent))
			{
				_bestResult = secondParent;
				_bestParents = (_firstParent, _secondParent);
			}

			_firstParent = firstParent;
			_secondParent = secondParent;

			Globals.Counter++;
		}

		public void NextGenerationWithParameters(List<Point> firstParent, List<Point> secondParent)
		{
			_points = firstParent;
			_firstParent = firstParent.Select(i => _points.IndexOf(i)).ToList();
			_secondParent = secondParent.Select(i => _points.IndexOf(i)).ToList();

			NextGeneration();
		}

		public void NextGeneration(object state = null)
        {

			var pointsAmount = _points.Count;
            var sequenceData = CreateSubsequence(pointsAmount);

            var firstParent = NextGenerationChild(_firstParent, _secondParent, sequenceData).ToList();
            var secondParent = NextGenerationChild(_secondParent, _firstParent, sequenceData).ToList();

            var bestResultDistance = PointUtils.GetTotalDistance(_points, _bestResult);
            if (bestResultDistance > PointUtils.GetTotalDistance(_points, firstParent))
            {
                _bestResult = firstParent;
                _bestParents = (_firstParent, _secondParent);
            }

            if (bestResultDistance > PointUtils.GetTotalDistance(_points, secondParent))
            {
                _bestResult = secondParent;
                _bestParents = (_firstParent, _secondParent);
            }

            _firstParent = firstParent;
            _secondParent = secondParent;

            Globals.Counter++;
        }

        private static IEnumerable<int> NextGenerationChild(
            IReadOnlyList<int> firstParent, IEnumerable<int> secondParent, (int, int) sequenceData)
        {
            var subsequence = new int[sequenceData.Item2];
            for (var i = 0; i < sequenceData.Item2; i++)
            {
                subsequence[i] = firstParent[sequenceData.Item1 + i];
            }

            return GetIndexesOfCrossedParents(
                firstParent.ToArray(), secondParent.ToList(), 
                subsequence.ToList(), sequenceData.Item1);
        }

        private static IEnumerable<int> GetIndexesOfCrossedParents(IReadOnlyList<int> firstParent, List<int> secondParent, List<int> seq, int seqAnchor)
        {
            var nextGeneration = new int[firstParent.Count];

            for (var i = 0; i < secondParent.Count; i++)
            {
                nextGeneration[i] = secondParent[i];
            }
            for (var i = seqAnchor; i < seqAnchor + seq.Count; i++)
            {
                nextGeneration[i] = firstParent[i];
            }
            
            for (var i = seqAnchor; i < seqAnchor + seq.Count; i++)
            {
                if (seq.Exists(x => x == secondParent[i])) continue;

                var marchingIndex = i;
                while (marchingIndex >= seqAnchor && marchingIndex < seqAnchor + seq.Count)
                {
                    var element = firstParent.ElementAt(marchingIndex);
                    marchingIndex = secondParent.FindIndex(x => x == element);
                }
                
                nextGeneration[marchingIndex] = secondParent[i];
            }

            return nextGeneration;
        }

        private static (int, int) CreateSubsequence(int sequenceLength)
        {
            var generator = new Random();
            
            var subsequenceLength = generator.Next(1, sequenceLength);
            var anchorIndex = generator.Next(0, sequenceLength - subsequenceLength);
            
            return (anchorIndex, subsequenceLength);
        }

        private static List<int> GetPermutation(int generationLength)
        {
            var numbers = Enumerable.Range(0, generationLength).ToList();
            var result = new List<int>();

            while (numbers.Count > 0)
            {
                var shift = new Random().Next(0, generationLength);
                
                result.Add(numbers[shift % numbers.Count]);
                numbers.Remove(result.Last());
            }

            return result;
        }
    }
}
