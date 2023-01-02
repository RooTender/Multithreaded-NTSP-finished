using Bridge;

namespace Calculator
{
    public class PMX
    {
        private List<int> _bestFirstParentIndexes;
        private List<int> _bestSecondParentIndexes;
        private readonly List<Point> _points;
        
        private IReadOnlyList<int> _bestResult;

        public PMX(List<Point> points)
        {
            _points = points;
            _bestResult = Enumerable.Range(0, points.Count).ToList();

            _bestFirstParentIndexes = GetPermutation(points.Count);
            _bestSecondParentIndexes = GetPermutation(points.Count);
        }

		public List<Point> BestGeneration()
        {
            return _bestResult.Select(i => _points[i]).ToList();
        }

        public void NextGeneration(object? _)
        {
            NextGeneration();
        }

        public void NextGeneration()
		{
			var pointsAmount = _points.Count;
			var sequenceData = CreateSubsequence(pointsAmount);

			var currentFirstParentIndexes = NextGenerationChild(_bestFirstParentIndexes, _bestSecondParentIndexes, sequenceData).ToList();
			var currentSecondParentIndexes = NextGenerationChild(_bestSecondParentIndexes, _bestFirstParentIndexes, sequenceData).ToList();

			var bestResultDistance = PointUtils.GetTotalDistance(_points, _bestResult);
			if (bestResultDistance > PointUtils.GetTotalDistance(_points, currentFirstParentIndexes))
			{
				_bestResult = currentFirstParentIndexes;
            }

			if (bestResultDistance > PointUtils.GetTotalDistance(_points, currentSecondParentIndexes))
			{
				_bestResult = currentSecondParentIndexes;
            }

			_bestFirstParentIndexes = currentFirstParentIndexes;
			_bestSecondParentIndexes = currentSecondParentIndexes;

            ++ParallelNTSP.CalculatedSolutions;
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
